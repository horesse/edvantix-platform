import { expect, test, type Route } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  INVOICE_ID,
  PAYMENT_ID,
  PERMS,
  STUDENT_ID,
  invoiceDetail,
  line,
  payment,
  reversalRow,
  studentRow,
} from "./fixtures";

const JSON_HEADERS = { "Content-Type": "application/json" } as const;

test.describe("/payments/invoices/:id — сторнирование оплаты", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/students/*", studentRow(STUDENT_ID, "Иванов Пётр"), {
      method: "GET",
    });
  });

  test("сторно-строка: отрицательная сумма + reversesId, без кнопки повторного сторно", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.paymentsView,
      PERMS.paymentsRevoke,
    ]);
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({
        status: "PartiallyPaid",
        total: 7200,
        paidAmount: 0,
        lines: [line()],
        payments: [payment(), reversalRow()],
      }),
      { method: "GET" },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);

    // The reversal row is flagged (badge) and its note shows.
    await expect(page.getByText("сторно")).toBeVisible();
    await expect(page.getByText("Ошибочный платёж")).toBeVisible();
    // Only the original (non-reversal) payment offers a "Сторнировать" action.
    await expect(page.getByRole("button", { name: "Сторнировать" })).toHaveCount(1);
  });

  test("«Сторнировать» скрыто без права StudentPayments.Revoke", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.paymentsView,
    ]);
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({
        status: "PartiallyPaid",
        paidAmount: 5000,
        lines: [line()],
        payments: [payment()],
      }),
      { method: "GET" },
    );

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await expect(page.getByRole("heading", { name: "Оплаты" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Сторнировать" })).toHaveCount(0);
  });

  test("сторнирование требует причину; POST /payments/{id}/reverse с note", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.invoicesView,
      PERMS.paymentsView,
      PERMS.paymentsRevoke,
    ]);
    await mockJsonResponse(
      page,
      `**/api/v1/student-invoices/${INVOICE_ID}`,
      invoiceDetail({
        status: "PartiallyPaid",
        paidAmount: 5000,
        lines: [line()],
        payments: [payment()],
      }),
      { method: "GET" },
    );

    let reqUrl = "";
    let body: Record<string, unknown> | null = null;
    await page.route("**/api/v1/payments/*/reverse", async (route: Route) => {
      reqUrl = route.request().url();
      body = route.request().postDataJSON() as Record<string, unknown>;
      await route.fulfill({
        status: 200,
        headers: JSON_HEADERS,
        body: JSON.stringify("reversal-id"),
      });
    });

    await page.goto(`/payments/invoices/${INVOICE_ID}`);
    await page.getByRole("button", { name: "Сторнировать" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Сторнировать оплату")).toBeVisible();

    // Reason is mandatory — submit stays disabled until it's filled.
    await expect(
      dialog.getByRole("button", { name: "Сторнировать", exact: true }),
    ).toBeDisabled();
    await dialog.getByLabel("Причина сторно").fill("Ошибка кассира");
    await expect(
      dialog.getByRole("button", { name: "Сторнировать", exact: true }),
    ).toBeEnabled();

    await dialog.getByRole("button", { name: "Сторнировать", exact: true }).click();

    await expect.poll(() => body).not.toBeNull();
    expect(body).toEqual({ note: "Ошибка кассира" });
    expect(reqUrl).toContain(`/payments/${PAYMENT_ID}/reverse`);
  });
});
