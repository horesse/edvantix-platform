import { expect, test, type Route } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  mockSchedulingRefs,
  PERMS,
  SESSION_ID,
  sessionDetail,
} from "./fixtures";

const JSON_HEADERS = { "Content-Type": "application/json" } as const;

test.describe("перенос занятия — 409 конфликт → диалог с force:true", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsView,
      PERMS.sessionsReschedule,
    ]);
    await mockSchedulingRefs(page);
    await mockJsonResponse(page, `**/api/v1/sessions/${SESSION_ID}`, sessionDetail());
  });

  test("первый перенос ловит 409, «Перенести всё равно» повторяет с force:true", async ({
    page,
  }) => {
    const bodies: Array<Record<string, unknown>> = [];

    // One handler: force:false → 409 with a described conflict; force:true → 200.
    await page.route(
      `**/api/v1/sessions/${SESSION_ID}/reschedule`,
      async (route: Route) => {
        const body = route.request().postDataJSON() as Record<string, unknown>;
        bodies.push(body);
        if (body.force === true) {
          await route.fulfill({
            status: 200,
            headers: JSON_HEADERS,
            body: JSON.stringify("60000000-0000-0000-0000-0000000000aa"),
          });
          return;
        }
        await route.fulfill({
          status: 409,
          headers: { "Content-Type": "application/problem+json" },
          body: JSON.stringify({
            title: "CustomException",
            status: 409,
            detail:
              "The new slot conflicts with an existing session. Pass force=true to override.",
            errors: [
              "Teacher conflicts with session 50000000-0000-0000-0000-0000000000ff at 2026-09-14T15:00:00.0000000Z.",
            ],
          }),
        });
      },
    );

    await page.goto(`/sessions/${SESSION_ID}`);
    await expect(
      page.getByRole("heading", { name: "Present Simple", level: 1 }),
    ).toBeVisible();

    // Open the reschedule dialog from the session card.
    await page.getByRole("button", { name: "Перенести" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Перенести занятие")).toBeVisible();

    // Move to a new day, then submit.
    await dialog.getByLabel("Дата").fill("2026-09-14");
    await dialog.getByRole("button", { name: "Перенести", exact: true }).click();

    // The 409 must surface, not be swallowed.
    await expect(
      dialog.getByText("Новый слот пересекается с другим занятием:"),
    ).toBeVisible();
    await expect(dialog.getByText(/Teacher conflicts with session/)).toBeVisible();

    // Retry with force.
    await dialog.getByRole("button", { name: "Перенести всё равно" }).click();

    await expect
      .poll(() => bodies.length)
      .toBeGreaterThanOrEqual(2);
    expect(bodies[0].force).toBe(false);
    expect(bodies[bodies.length - 1].force).toBe(true);
    // Same target slot on both attempts.
    expect(bodies[bodies.length - 1].newStartUtc).toBe(bodies[0].newStartUtc);
  });
});
