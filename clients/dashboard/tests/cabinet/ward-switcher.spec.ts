import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import {
  CABINET_USER,
  PERMS,
  mockScope,
  mockTenantSettings,
  session,
} from "./fixtures";

test.describe("Переключатель подопечных представителя", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, CABINET_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [PERMS.sessionsViewOwn]);
    await mockTenantSettings(page);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([]));
  });

  test("wardStudentIds → выбор подопечного уходит в запрос /sessions/my как studentId", async ({
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
    await mockJsonResponse(page, "**/api/v1/sessions/my?**", [session()]);

    await page.goto("/my/schedule");

    // Занятие из общего ответа видно.
    await expect(page.getByText("Present Simple")).toBeVisible();

    const switcher = page.getByRole("group", { name: "Подопечный" });
    await expect(switcher.getByRole("button", { name: "Пётр Иванов" })).toBeVisible();

    // Выбор конкретного подопечного → рефетч /sessions/my с studentId=w-1.
    const req = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/sessions/my?") && r.url().includes("studentId=w-1"),
    );
    await switcher.getByRole("button", { name: "Пётр Иванов" }).click();
    await req;
  });

  test("ученик без подопечных — переключатель не показывается", async ({ page }) => {
    await mockScope(page, { studentId: "stu-1" });
    await mockJsonResponse(page, "**/api/v1/sessions/my?**", []);

    await page.goto("/my/schedule");

    await expect(page.getByRole("heading", { name: "Моё расписание" })).toBeVisible();
    await expect(page.getByRole("group", { name: "Подопечный" })).toHaveCount(0);
  });
});
