import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  GROUP_ID,
  mockSchedulingRefs,
  PERMS,
  ROOM_ID,
  SESSION_ID,
  TEACHER_ID,
} from "./fixtures";

// An event at noon UTC today — always inside the current week/month view.
function todayNoon(): { start: string; end: string } {
  const d = new Date();
  d.setUTCHours(12, 0, 0, 0);
  const e = new Date(d.getTime() + 90 * 60 * 1000);
  return { start: d.toISOString(), end: e.toISOString() };
}

test.describe("/schedule — календарь", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.sessionsView,
      PERMS.sessionsReschedule,
    ]);
    await mockSchedulingRefs(page);

    const { start, end } = todayNoon();
    await mockJsonResponse(page, "**/api/v1/sessions/calendar**", [
      {
        sessionId: SESSION_ID,
        studyGroupId: GROUP_ID,
        teacherId: TEACHER_ID,
        roomId: ROOM_ID,
        startUtc: start,
        endUtc: end,
        status: "Planned",
        topic: "Present Simple",
      },
    ]);
  });

  test("рисует занятие; клик открывает карточку", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/sessions/${SESSION_ID}`, {
      id: SESSION_ID,
      studyGroupId: GROUP_ID,
      lessonId: null,
      teacherId: TEACHER_ID,
      roomId: ROOM_ID,
      startUtc: todayNoon().start,
      endUtc: todayNoon().end,
      status: "Planned",
      resolvedTopic: "Present Simple",
      meetingUrl: null,
      cancelReason: null,
      rescheduledFromId: null,
      scheduleTemplateId: null,
      teacherComment: null,
      attendance: [],
    });

    await page.goto("/schedule");
    await expect(page.getByRole("heading", { name: "Календарь занятий" })).toBeVisible();

    const event = page.locator(".fc-event", { hasText: "ENG-A1" });
    await expect(event.first()).toBeVisible();

    await event.first().click();
    await expect(page).toHaveURL(new RegExp(`/sessions/${SESSION_ID}$`));
  });

  test("запланированное занятие помечено как перетаскиваемое (drag → reschedule)", async ({
    page,
  }) => {
    // FullCalendar tags an editable event with `fc-event-draggable`; that is the
    // wiring that fires `eventDrop` → POST /sessions/{id}/reschedule. The full
    // reschedule + 409-conflict + force:true flow (shared with this handler) is
    // asserted in reschedule.spec.ts, which does not depend on a synthetic
    // FullCalendar drag gesture.
    await page.goto("/schedule");
    const event = page.locator(".fc-event", { hasText: "ENG-A1" }).first();
    await expect(event).toBeVisible();
    await expect(event).toHaveClass(/fc-event-draggable/);
  });

  test("без права Reschedule занятие не перетаскивается", async ({ page }) => {
    await page.route("**/api/v1/identity/permissions", (r) =>
      r.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify([PERMS.sessionsView]),
      }),
    );
    await page.goto("/schedule");
    const event = page.locator(".fc-event", { hasText: "ENG-A1" }).first();
    await expect(event).toBeVisible();
    await expect(event).not.toHaveClass(/fc-event-draggable/);
  });
});
