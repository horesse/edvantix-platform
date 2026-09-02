import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  CABINET_USER,
  PERMS,
  invoice,
  mockMaterialsAccess,
  mockScope,
  mockTenantSettings,
} from "./fixtures";

// EDX-015 — «Доступ к материалам ограничен из-за задолженности».
// Плашка кабинета управляется только ответом GET /student-invoices/my/materials-access;
// фронт не пересчитывает правило.

const NOTICE = /Доступ к материалам ограничен из-за задолженности/i;

test.describe("Плашка задолженности в кабинете", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, CABINET_USER);
    await installShellMocks(page);
    await mockTenantSettings(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsViewOwn,
      PERMS.invoicesViewOwn,
    ]);
    await mockScope(page, { studentId: "stu-1" });
    await mockJsonResponse(page, "**/api/v1/sessions/my?**", []);
    await mockJsonResponse(page, "**/api/v1/student-invoices/my**", [
      invoice({ id: "i-1", status: "Issued", isOverdue: true, dueDate: "2026-08-01" }),
    ]);
  });

  test("restricted:true → плашка на /my со ссылкой на счета", async ({ page }) => {
    await mockMaterialsAccess(page, { restricted: true, overdueSince: "2026-08-01" });

    await page.goto("/my");

    await expect(page.getByText(NOTICE)).toBeVisible();
    await expect(page.getByRole("link", { name: "Перейти к счетам" })).toHaveAttribute(
      "href",
      "/my/invoices",
    );
  });

  test("restricted:false → плашки нет", async ({ page }) => {
    await mockMaterialsAccess(page, { restricted: false });

    await page.goto("/my");

    await expect(page.getByRole("heading", { name: /здравствуйте, alice/i })).toBeVisible();
    await expect(page.getByText(NOTICE)).toHaveCount(0);
  });

  test("restricted:true → плашка и на /my/invoices", async ({ page }) => {
    await mockMaterialsAccess(page, { restricted: true });

    await page.goto("/my/invoices");

    await expect(page.getByRole("heading", { name: "Мои счета" })).toBeVisible();
    await expect(page.getByText(NOTICE)).toBeVisible();
  });
});
