import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import {
  CABINET_USER,
  PERMS,
  invoice,
  mockScope,
  mockTenantSettings,
  session,
} from "./fixtures";

test.describe("/my/schedule — моё расписание", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, CABINET_USER);
    await installShellMocks(page);
    await mockTenantSettings(page);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([]));
  });

  test("рендерит «свои» занятия из мока /sessions/my", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [PERMS.sessionsViewOwn]);
    await mockScope(page, { teacherId: "t-1" });
    await mockJsonResponse(page, "**/api/v1/sessions/my?**", [
      session({ id: "s-a", topic: "Present Simple" }),
      session({ id: "s-b", topic: "Past Simple", startUtc: "2026-09-03T09:00:00Z" }),
    ]);

    await page.goto("/my/schedule");

    await expect(page.getByRole("heading", { name: "Моё расписание" })).toBeVisible();
    await expect(page.getByText("Present Simple")).toBeVisible();
    await expect(page.getByText("Past Simple")).toBeVisible();
  });

  test("без права Sessions.ViewOwn — заглушка «нет доступа»", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", []);
    await mockScope(page, { teacherId: "t-1" });

    await page.goto("/my/schedule");

    await expect(page.getByText(/нет доступа/i)).toBeVisible();
  });
});

test.describe("/my/invoices — мои счета", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, CABINET_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [PERMS.invoicesViewOwn]);
  });

  test("ученик — рендерит свои счета из мока /student-invoices/my", async ({ page }) => {
    await mockScope(page, { studentId: "stu-1" });
    await mockJsonResponse(page, "**/api/v1/student-invoices/my**", [
      invoice({ id: "i-1", number: "SI-2026-0042", status: "Issued" }),
      invoice({
        id: "i-2",
        number: "SI-2026-0043",
        status: "PartiallyPaid",
        paidAmount: 2000,
      }),
    ]);

    await page.goto("/my/invoices");

    await expect(page.getByRole("heading", { name: "Мои счета" })).toBeVisible();
    await expect(page.getByText("SI-2026-0042")).toBeVisible();
    await expect(page.getByText("SI-2026-0043")).toBeVisible();
    // Столбца «Ученик» у ученика без подопечных нет.
    await expect(page.getByText("Ученик", { exact: true })).toHaveCount(0);
  });

  test("представитель — показывает столбец «Ученик» и фильтрует по выбранному подопечному", async ({
    page,
  }) => {
    await mockScope(page, { guardianId: "g-1", wardStudentIds: ["w-1", "w-2"] });
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
    await mockJsonResponse(page, "**/api/v1/student-invoices/my**", [
      invoice({ id: "i-1", number: "SI-W1", studentId: "w-1" }),
      invoice({ id: "i-2", number: "SI-W2", studentId: "w-2" }),
    ]);

    await page.goto("/my/invoices");

    await expect(page.getByText("SI-W1")).toBeVisible();
    await expect(page.getByText("SI-W2")).toBeVisible();
    await expect(page.getByText("Ученик", { exact: true })).toBeVisible();

    // Выбор подопечного w-1 → в списке остаётся только его счёт (клиентский фильтр).
    await page
      .getByRole("group", { name: "Подопечный" })
      .getByRole("button", { name: "Пётр Иванов" })
      .click();
    await expect(page.getByText("SI-W1")).toBeVisible();
    await expect(page.getByText("SI-W2")).toHaveCount(0);
  });
});
