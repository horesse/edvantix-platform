import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import {
  attendanceRow,
  mockSchedulingRefs,
  PERMS,
  SESSION_ID,
  STU_1,
  STU_2,
  STU_3,
  sessionDetail,
} from "./fixtures";

test.describe("/attendance — таблица посещаемости", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.attendanceView,
      PERMS.attendanceMark,
      PERMS.sessionsView,
    ]);
    await mockSchedulingRefs(page);
    await mockJsonResponse(
      page,
      "**/api/v1/sessions?**",
      paged([sessionDetail({ status: "Held" })]),
    );
  });

  test("массовая отметка: дефолт «Был», отправляем только исключения одним PUT", async ({
    page,
  }) => {
    await mockJsonResponse(page, `**/api/v1/sessions/${SESSION_ID}/attendance`, [
      attendanceRow(STU_1, "Present"),
      attendanceRow(STU_2, "Present"),
      attendanceRow(STU_3, "Present"),
    ]);
    // Method-scoped so the GET above still serves the grid.
    await mockJsonResponse(page, `**/api/v1/sessions/${SESSION_ID}/attendance`, "", {
      method: "PUT",
      status: 204,
    });

    await page.goto(`/attendance?sessionId=${SESSION_ID}`);

    // All three rows render, each defaulting to «Был» (Present).
    await expect(page.getByLabel("Иванов Пётр: Был")).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    await expect(page.getByLabel("Петров Иван: Был")).toHaveAttribute(
      "aria-pressed",
      "true",
    );

    // Nothing changed yet.
    await expect(page.getByText("Изменений нет")).toBeVisible();
    await expect(page.getByRole("button", { name: "Сохранить" })).toBeDisabled();

    // Mark exactly one exception.
    await page.getByLabel("Петров Иван: Не был").click();
    await expect(page.getByText("Будет отправлено записей: 1")).toBeVisible();

    const putReq = page.waitForRequest(
      (r) =>
        r.url().includes(`/sessions/${SESSION_ID}/attendance`) &&
        r.method() === "PUT",
    );
    await page.getByRole("button", { name: "Сохранить" }).click();
    const req = await putReq;

    const body = req.postDataJSON();
    expect(Array.isArray(body)).toBe(true);
    expect(body).toEqual([
      { studentId: STU_2, status: "Absent", comment: null },
    ]);
  });

  test("без выбранного занятия — подсказка выбрать занятие", async ({ page }) => {
    await page.goto("/attendance");
    await expect(page.getByText("Занятие не выбрано")).toBeVisible();
  });
});
