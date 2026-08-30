import { expect, test, type Route } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  INVOICE_ID,
  PERMS,
  STUDENT_ID,
  invoiceDetail,
  line,
  studentRow,
} from "./fixtures";

const JSON_HEADERS = { "Content-Type": "application/json" } as const;
const todayIso = () => new Date().toISOString().slice(0, 10);

test.describe("/payments/invoices/:id — подтверждение оплаты", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/students/*", studentRow(STUDENT_ID, "Иванов Пётр"), {
      method: "GET",
    });
  });

  test("кнопка «Подтвердить оплату» скрыта без права StudentPayments.Confirm", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.paymentsView,
    ]);
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({ status: "Issued", lines: [line()], payments: [] }),
      { method: "GET" },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await expect(page.getByRole("heading", { name: "Оплаты" })).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Подтвердить оплату" }),
    ).toHaveCount(0);
  });

  test("подтверждение: POST .../payments, переплата не блокируется", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.paymentsView,
      PERMS.paymentsConfirm,
    ]);
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({
        status: "Issued",
        total: 7200,
        paidAmount: 0,
        lines: [line()],
        payments: [],
      }),
      { method: "GET" },
    );

    let body: Record<string, unknown> | null = null;
    await page.route(
      `**/api/v1/student-invoices/${INVOICE_ID}/payments`,
      async (route: Route) => {
        if (route.request().method() !== "POST") {
          await route.fallback();
          return;
        }
        body = route.request().postDataJSON() as Record<string, unknown>;
        await route.fulfill({
          status: 200,
          headers: JSON_HEADERS,
          body: JSON.stringify("new-payment-id"),
        });
      },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await page.getByRole("button", { name: "Подтвердить оплату" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Подтвердить оплату")).toBeVisible();

    // Amount pre-fills to the outstanding balance; override with an overpayment.
    await dialog.getByLabel("Сумма").fill("10000");
    await expect(
      dialog.getByText("Сумма больше остатка долга — разница уйдёт в переплату (аванс)."),
    ).toBeVisible();
    // Overpayment must NOT disable submit.
    await expect(
      dialog.getByRole("button", { name: "Подтвердить", exact: true }),
    ).toBeEnabled();

    await dialog.getByRole("button", { name: "Подтвердить", exact: true }).click();

    await expect.poll(() => body).not.toBeNull();
    expect(body).toMatchObject({
      amount: 10000,
      paidOn: todayIso(),
      method: "Cash",
      reference: null,
      proofFileId: null,
      note: null,
    });
  });
});
