import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import { mockSchedulingRefs, PERMS, SESSION_ID, sessionDetail } from "./fixtures";

test.describe("отмена занятия", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsView,
      PERMS.sessionsCancel,
    ]);
    await mockSchedulingRefs(page);
  });

  test("отмена шлёт причину и переводит карточку в read-only", async ({ page }) => {
    let calls = 0;
    await page.route(`**/api/v1/sessions/${SESSION_ID}`, async (route) => {
      if (route.request().method() !== "GET") {
        await route.fallback();
        return;
      }
      calls += 1;
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(
          calls === 1
            ? sessionDetail()
            : sessionDetail({ status: "Cancelled", cancelReason: "Праздник" }),
        ),
      });
    });
    await mockJsonResponse(page, `**/api/v1/sessions/${SESSION_ID}/cancel`, "", {
      method: "POST",
      status: 204,
    });

    await page.goto(`/sessions/${SESSION_ID}`);
    await expect(
      page.getByRole("heading", { name: "Present Simple", level: 1 }),
    ).toBeVisible();

    await page.getByRole("button", { name: "Отменить" }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Причина").fill("Праздник");

    const postReq = page.waitForRequest(
      (r) =>
        r.url().includes(`/sessions/${SESSION_ID}/cancel`) && r.method() === "POST",
    );
    await dialog.getByRole("button", { name: "Отменить занятие" }).click();
    const req = await postReq;
    expect(req.postDataJSON()).toEqual({ reason: "Праздник" });

    // After the refetch the card is read-only.
    await expect(
      page.getByText("карточка доступна только для чтения"),
    ).toBeVisible();
    await expect(page.getByText("Праздник")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Отменить", exact: true }),
    ).toHaveCount(0);
  });
});
