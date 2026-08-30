import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  attendanceRow,
  mockSchedulingRefs,
  PERMS,
  SESSION_ID,
  STU_1,
  sessionDetail,
} from "./fixtures";

test.describe("«Провести» занятие", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsView,
      PERMS.sessionsUpdate,
    ]);
    await mockSchedulingRefs(page);
  });

  test("после hold экран перезапрашивает занятие и показывает созданную посещаемость", async ({
    page,
  }) => {
    let getCalls = 0;
    await page.route(`**/api/v1/sessions/${SESSION_ID}`, async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      getCalls += 1;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(
          getCalls === 1
            ? sessionDetail({ status: "Planned", attendance: [] })
            : sessionDetail({
                status: "Held",
                attendance: [attendanceRow(STU_1, "Present")],
              }),
        ),
      });
    });

    const holdReq = page.waitForRequest(
      (r) => r.url().includes(`/sessions/${SESSION_ID}/hold`) && r.method() === "POST",
    );
    await mockJsonResponse(page, `**/api/v1/sessions/${SESSION_ID}/hold`, "", {
      method: "POST",
      status: 204,
    });

    await page.goto(`/sessions/${SESSION_ID}`);
    await expect(page.getByText("Записей о посещаемости пока нет.")).toBeVisible();

    await page.getByRole("button", { name: "Провести" }).click();
    await holdReq;

    // The re-fetched session shows Held + the server-seeded attendance row.
    await expect(page.getByText("Проведено").first()).toBeVisible();
    await expect(page.getByText("Иванов Пётр")).toBeVisible();
    expect(getCalls).toBeGreaterThanOrEqual(2);
  });
});
