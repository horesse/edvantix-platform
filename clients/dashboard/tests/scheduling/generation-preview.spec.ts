import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import {
  groupDetail,
  GROUP_ID,
  mockSchedulingRefs,
  OTHER_SESSION_ID,
  PERMS,
  TEMPLATE_ID,
  template,
} from "./fixtures";

const PREVIEW = {
  scheduleTemplateId: TEMPLATE_ID,
  toCreate: [
    { localDate: "2026-09-14", startUtc: "2026-09-14T18:00:00Z", endUtc: "2026-09-14T19:30:00Z" },
    { localDate: "2026-09-21", startUtc: "2026-09-21T18:00:00Z", endUtc: "2026-09-21T19:30:00Z" },
  ],
  skipped: [
    { localDate: "2026-09-28", reason: "NonWorkingDay", conflicts: [] },
    {
      localDate: "2026-10-05",
      reason: "Conflict",
      conflicts: [
        {
          type: "Teacher",
          conflictingSessionId: OTHER_SESSION_ID,
          conflictingSessionStartUtc: "2026-10-05T18:00:00Z",
        },
      ],
    },
  ],
};

test.describe("/study-groups/:id/schedule — предпросмотр генерации", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.templatesView,
      PERMS.templatesManage,
      PERMS.sessionsGenerate,
    ]);
    await mockSchedulingRefs(page);
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GROUP_ID}/schedule-templates`,
      [template()],
    );
    await mockJsonResponse(page, `**/api/v1/study-groups/${GROUP_ID}`, groupDetail());
  });

  test("показывает ToCreate/Skipped, конфликт с SessionConflictDto, затем применяет", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/schedule-templates/${TEMPLATE_ID}/preview**`,
      PREVIEW,
      { method: "POST" },
    );
    await mockJsonResponse(
      page,
      `**/api/v1/schedule-templates/${TEMPLATE_ID}/generate**`,
      { scheduleTemplateId: TEMPLATE_ID, createdSessionIds: ["s1", "s2"], skipped: PREVIEW.skipped },
      { method: "POST" },
    );

    await page.goto(`/study-groups/${GROUP_ID}/schedule`);
    await expect(page.getByText(/Понедельник, 18:00 · 90 мин/)).toBeVisible();

    await page.getByRole("button", { name: "Предпросмотр" }).click();

    await expect(page.getByText("К созданию: 2")).toBeVisible();
    await expect(page.getByText("Пропущено: 2")).toBeVisible();
    await expect(page.getByText("Нерабочий день")).toBeVisible();
    await expect(page.getByText("Конфликт ресурса")).toBeVisible();
    await expect(page.getByText(/Преподаватель занят/)).toBeVisible();

    const genReq = page.waitForRequest(
      (r) =>
        r.url().includes(`/schedule-templates/${TEMPLATE_ID}/generate`) &&
        r.method() === "POST",
    );
    await page.getByRole("button", { name: "Применить (2)" }).click();
    await genReq;

    // Panel closes after a successful generate.
    await expect(page.getByText("Предпросмотр генерации")).toHaveCount(0);
  });
});
