import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { CABINET_USER, PERMS, mockScope, mockTenantSettings } from "./fixtures";

test.describe("Стартовая страница по роли — индексный редирект", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, CABINET_USER);
    await installShellMocks(page);
  });

  test("менеджер (есть Students.View) остаётся на обзоре школы «/»", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [PERMS.studentsView]);
    await mockScope(page, {}); // пустой scope
    // Зависимости обзора школы (как в tests/overview).
    await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", {
      id: "sub-1",
      tenantId: "acme",
      planId: "plan-scale",
      planKey: "Scale",
      startUtc: "2026-01-01T00:00:00Z",
      endUtc: null,
      status: "Active",
    });
    await mockJsonResponse(page, "**/api/v1/audits**", paged([]));

    await page.goto("/");

    await expect(
      page.getByRole("heading", { name: /good (morning|afternoon|evening)/i }),
    ).toBeVisible();
    await expect(page).toHaveURL(/\/$/);
  });

  test("преподаватель (scope.teacherId) → редирект на /my, лендинг преподавателя", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [PERMS.sessionsViewOwn]);
    await mockScope(page, { teacherId: "t-1" });
    await mockTenantSettings(page);
    await mockJsonResponse(page, "**/api/v1/sessions/my?**", []);
    await mockJsonResponse(page, "**/api/v1/study-groups/my", []);

    await page.goto("/");

    await expect(page).toHaveURL(/\/my$/);
    await expect(page.getByRole("heading", { name: /здравствуйте, alice/i })).toBeVisible();
    // У преподавателя блок «Мои группы», а не «Мои счета».
    await expect(page.getByRole("heading", { name: "Мои группы" })).toBeVisible();
    await expect(page.getByRole("group", { name: "Подопечный" })).toHaveCount(0);
  });

  test("представитель (guardianId + wardStudentIds) → /my с переключателем подопечных", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsViewOwn,
      PERMS.invoicesViewOwn,
    ]);
    await mockScope(page, { guardianId: "g-1", wardStudentIds: ["w-1", "w-2"] });
    await mockTenantSettings(page);
    await mockJsonResponse(page, "**/api/v1/sessions/my?**", []);
    await mockJsonResponse(page, "**/api/v1/student-invoices/my**", []);
    await mockJsonResponse(page, "**/api/v1/students/w-1", {
      id: "w-1",
      displayName: "Пётр Иванов",
      firstName: "Пётр",
      lastName: "Иванов",
      birthDate: "2012-01-01",
      phone: "",
      email: "",
      status: "Active",
      managerUserId: "m-1",
      enrolledAtUtc: "2026-01-01T00:00:00Z",
      createdAtUtc: "2026-01-01T00:00:00Z",
      guardianCount: 1,
      noteCount: 0,
    });
    await mockJsonResponse(page, "**/api/v1/students/w-2", "Forbidden", { status: 403 });

    await page.goto("/");

    await expect(page).toHaveURL(/\/my$/);
    const switcher = page.getByRole("group", { name: "Подопечный" });
    await expect(switcher).toBeVisible();
    await expect(switcher.getByRole("button", { name: "Все" })).toBeVisible();
    await expect(switcher.getByRole("button", { name: "Пётр Иванов" })).toBeVisible();
    // ФИО второго подопечного недоступно (403) → запасная подпись.
    await expect(switcher.getByRole("button", { name: "Подопечный 2" })).toBeVisible();
  });
});
