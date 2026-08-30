import { expect, test, type Route } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import {
  DRAFT_ID,
  GROUP_ID,
  INVOICE_ID,
  PERMS,
  invoice,
  mockPaymentsRefs,
} from "./fixtures";

const JSON_HEADERS = { "Content-Type": "application/json" } as const;

test.describe("/payments/invoices — список счетов учеников", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.invoicesCreate,
      PERMS.invoicesIssue,
    ]);
    await mockPaymentsRefs(page);
  });

  test("рендерит список; колонка «просрочен» берётся из бэкенда, не пересчитывается", async ({
    page,
  }) => {
    // One invoice with a long-past dueDate but isOverdue:false — must NOT show
    // "просрочен". One with a far-future dueDate but isOverdue:true — must show it.
    await mockJsonResponse(
      page,
      "**/api/v1/student-invoices?**",
      paged([
        invoice({
          id: INVOICE_ID,
          number: "SI-PAST",
          dueDate: "2020-01-01",
          isOverdue: false,
          status: "Issued",
        }),
        invoice({
          id: DRAFT_ID,
          number: "SI-FUTURE",
          dueDate: "2099-01-01",
          isOverdue: true,
          status: "Issued",
        }),
      ]),
    );

    await page.goto("/payments/invoices");

    // Desktop row + mobile card both render the number — assert .last() (desktop).
    await expect(page.getByText("SI-PAST").last()).toBeVisible();
    await expect(page.getByText("SI-FUTURE").last()).toBeVisible();
    // Only the backend-flagged one renders the badge (desktop row + mobile card).
    await expect(page.getByText("просрочен")).toHaveCount(2);
  });

  test("фильтр по статусу уходит в запрос как status=Draft", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/student-invoices?**",
      paged([invoice()]),
    );

    await page.goto("/payments/invoices");
    await expect(page.getByText("SI-2026-0007").last()).toBeVisible();

    const req = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/student-invoices?") &&
        r.url().includes("status=Draft"),
    );
    await page.getByRole("button", { name: "Черновики" }).click();
    await req;
  });

  test("мастер массового выставления: issueImmediately:false, список созданных id", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/student-invoices?**",
      paged([invoice()]),
    );

    const bodies: Array<Record<string, unknown>> = [];
    await page.route(
      "**/api/v1/student-invoices/bulk-generate",
      async (route: Route) => {
        bodies.push(route.request().postDataJSON() as Record<string, unknown>);
        await route.fulfill({
          status: 200,
          headers: JSON_HEADERS,
          body: JSON.stringify([INVOICE_ID, DRAFT_ID]),
        });
      },
    );

    await page.goto("/payments/invoices");
    await page.getByRole("button", { name: "Массовое выставление" }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Массовое выставление счетов")).toBeVisible();

    // Step 1 — group. The trigger's accessible name is its <Field> label.
    await dialog.getByLabel("Учебная группа").click();
    await page.getByRole("menuitemradio", { name: /Английский A1/ }).click();
    await dialog.getByRole("button", { name: "Далее" }).click();

    // Step 2 — period + due date.
    await dialog.getByLabel("Период с").fill("2026-09-01");
    await dialog.getByLabel("Период по").fill("2026-09-30");
    await dialog.getByLabel("Срок оплаты").fill("2026-09-15");
    await dialog.getByRole("button", { name: "Далее" }).click();

    // Step 3 — review → generate.
    await expect(dialog.getByText("Создать черновики")).toBeVisible();
    await dialog.getByRole("button", { name: "Создать черновики" }).click();

    // Body carries issueImmediately:false explicitly.
    await expect.poll(() => bodies.length).toBeGreaterThan(0);
    expect(bodies[0]).toMatchObject({
      studyGroupId: GROUP_ID,
      periodFrom: "2026-09-01",
      periodTo: "2026-09-30",
      dueDate: "2026-09-15",
      issueImmediately: false,
    });

    // Result step lists both returned ids as links to their cards.
    await expect(
      dialog.getByRole("link", { name: INVOICE_ID }),
    ).toBeVisible();
    await expect(dialog.getByRole("link", { name: DRAFT_ID })).toBeVisible();
  });

  test("массовое выставление отмеченных черновиков — POST bulk-issue", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/student-invoices?**",
      paged([
        invoice({ id: DRAFT_ID, number: "SI-DRAFT-1", status: "Draft" }),
        invoice({ id: INVOICE_ID, number: "SI-ISSUED", status: "Issued" }),
      ]),
    );

    const bodies: Array<Record<string, unknown>> = [];
    await page.route(
      "**/api/v1/student-invoices/bulk-issue",
      async (route: Route) => {
        bodies.push(route.request().postDataJSON() as Record<string, unknown>);
        await route.fulfill({
          status: 200,
          headers: JSON_HEADERS,
          body: JSON.stringify([DRAFT_ID]),
        });
      },
    );

    await page.goto("/payments/invoices");
    await page
      .getByRole("checkbox", { name: "Отметить счёт SI-DRAFT-1" })
      .check();
    await expect(page.getByText("Отмечено черновиков:")).toBeVisible();
    await page.getByRole("button", { name: "Выставить отмеченные" }).click();

    await expect.poll(() => bodies.length).toBeGreaterThan(0);
    expect(bodies[0].invoiceIds).toEqual([DRAFT_ID]);
    expect(typeof bodies[0].issuedOn).toBe("string");
  });
});
