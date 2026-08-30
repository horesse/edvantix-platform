import { expect, test, type Route } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  INVOICE_ID,
  PERMS,
  STUDENT_ID,
  invoiceDetail,
  line,
  studentRow,
  tariff,
} from "./fixtures";

const JSON_HEADERS = { "Content-Type": "application/json" } as const;

test.describe("/payments/invoices/:id — выставление счёта", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.invoicesCreate,
      PERMS.invoicesIssue,
      PERMS.invoicesCancel,
    ]);
    await mockJsonResponse(page, "**/api/v1/students/*", studentRow(STUDENT_ID, "Иванов Пётр"), {
      method: "GET",
    });
    await mockJsonResponse(page, "**/api/v1/tariffs**", [tariff()]);
  });

  test("пустой черновик: кнопка «Выставить» заблокирована", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({ status: "Draft", lines: [], total: 0, payments: [] }),
      { method: "GET" },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await expect(page.getByText("SI-2026-0007").first()).toBeVisible();

    // Editable empty draft: the line editor shows its empty hint, and the
    // "Выставить" action is disabled client-side (server would 409 anyway).
    await expect(
      page.getByText(
        "Пока нет строк — добавьте хотя бы одну, чтобы счёт можно было выставить.",
      ),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Выставить" })).toBeDisabled();
  });

  test("сервер возвращает 409 на выставление — ошибка не проглатывается", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({ status: "Draft", lines: [line()], total: 7200, payments: [] }),
      { method: "GET" },
    );
    await mockProblemDetails(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}/issue`,
      409,
      { title: "CustomException", detail: "Cannot issue an invoice with no lines." },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await page.getByRole("button", { name: "Выставить" }).click();

    await expect(page.getByText(/Не удалось выставить счёт/)).toBeVisible();
  });

  test("не-Draft: редактор строк заблокирован (только чтение)", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({ status: "Issued", lines: [line()], total: 7200, payments: [] }),
      { method: "GET" },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await expect(page.getByText("Строки счёта").first()).toBeVisible();

    // Read-only table renders (Итого row), and no editing affordances.
    await expect(page.getByText("Итого")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Добавить строку" }),
    ).toHaveCount(0);
    await expect(
      page.getByRole("button", { name: "Сохранить строки" }),
    ).toHaveCount(0);
    // Issuing a non-draft is not offered.
    await expect(page.getByRole("button", { name: "Выставить" })).toHaveCount(0);
  });

  test("правка строк черновика: сохранение шлёт ВЕСЬ набор одним PUT (ReplaceLines)", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({
        status: "Draft",
        lines: [line({ description: "Занятия за сентябрь", quantity: 8, unitPrice: 900 })],
        total: 7200,
        payments: [],
      }),
      { method: "GET" },
    );

    let putBody: Record<string, unknown> | null = null;
    await page.route(
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      async (route: Route) => {
        if (route.request().method() !== "PUT") {
          await route.fallback();
          return;
        }
        putBody = route.request().postDataJSON() as Record<string, unknown>;
        await route.fulfill({ status: 204, headers: JSON_HEADERS, body: "" });
      },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await expect(
      page.getByText("Черновик — правьте строки и сохраняйте весь набор целиком."),
    ).toBeVisible();

    // Add a second line and fill it.
    await page.getByRole("button", { name: "Добавить строку" }).click();
    await page.getByLabel("Описание строки 2").fill("Вступительный взнос");
    await page.getByLabel("Количество строки 2").fill("1");
    await page.getByLabel("Цена строки 2").fill("500");

    await page.getByRole("button", { name: "Сохранить строки" }).click();

    await expect.poll(() => putBody).not.toBeNull();
    const body = putBody as unknown as {
      lines: Array<Record<string, unknown>>;
      periodFrom: string;
      periodTo: string;
      dueDate: string;
    };
    expect(Array.isArray(body.lines)).toBe(true);
    expect(body.lines).toHaveLength(2);
    expect(body.lines[0]).toMatchObject({
      description: "Занятия за сентябрь",
      quantity: 8,
      unitPrice: 900,
    });
    expect(body.lines[1]).toMatchObject({
      description: "Вступительный взнос",
      quantity: 1,
      unitPrice: 500,
    });
    // The PUT round-trips the invoice's own required fields too.
    expect(body.periodFrom).toBe("2026-09-01");
    expect(body.periodTo).toBe("2026-09-30");
    expect(body.dueDate).toBe("2026-09-15");
  });
});
