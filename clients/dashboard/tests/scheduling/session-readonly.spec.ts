import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import { mockSchedulingRefs, PERMS, SESSION_ID, sessionDetail } from "./fixtures";

test.describe("карточка занятия — read-only после терминального статуса", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsView,
      PERMS.sessionsUpdate,
      PERMS.sessionsCancel,
      PERMS.sessionsReschedule,
    ]);
    await mockSchedulingRefs(page);
  });

  for (const status of ["Held", "Cancelled", "Rescheduled"] as const) {
    test(`${status}: нет действий жизненного цикла`, async ({ page }) => {
      await mockJsonResponse(
        page,
        `**/api/v1/sessions/${SESSION_ID}`,
        sessionDetail({ status }),
      );
      await page.goto(`/sessions/${SESSION_ID}`);

      await expect(
        page.getByRole("heading", { name: "Present Simple", level: 1 }),
      ).toBeVisible();
      await expect(
        page.getByText("карточка доступна только для чтения"),
      ).toBeVisible();
      await expect(page.getByRole("button", { name: "Провести" })).toHaveCount(0);
      await expect(
        page.getByRole("button", { name: "Перенести" }),
      ).toHaveCount(0);
      await expect(
        page.getByRole("button", { name: "Отменить", exact: true }),
      ).toHaveCount(0);
    });
  }
});
