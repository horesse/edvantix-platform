import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const GID = "a0000000-0000-0000-0000-000000000001";
const COURSE_ID = "c0000000-0000-0000-0000-000000000001";
const TEACHER_ID = "70000000-0000-0000-0000-000000000001";

function detail() {
  return {
    id: GID,
    code: "ENG-A1",
    name: "Английский A1 · утро",
    courseId: COURSE_ID,
    primaryTeacherId: TEACHER_ID,
    format: "Offline",
    capacity: 8,
    activeEnrollmentCount: 0,
    startDate: "2026-02-01",
    endDate: null,
    status: "Active",
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-01-10T00:00:00Z",
    enrollments: [],
    teachers: [],
  };
}

const PERMS = [
  "Permissions.StudyGroups.StudyGroups.View",
  "Permissions.StudyGroups.Enrollments.View",
  "Permissions.Scheduling.Sessions.View",
];

test.describe("study-groups/:id — прогресс по программе (EDX-019)", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/students?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/teachers?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([]));
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}`, detail());
  });

  test("renders 'N из M уроков' with a progress bar", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}/course-progress`, {
      studyGroupId: GID,
      courseId: COURSE_ID,
      passedLessons: 3,
      totalLessons: 10,
    });

    await page.goto(`/study-groups/${GID}`);

    await expect(page.getByText("Пройдено 3 из 10 уроков")).toBeVisible();
    await expect(page.getByText("30%")).toBeVisible();
    await expect(page.getByRole("progressbar")).toHaveAttribute("aria-valuenow", "3");
  });

  test("empty course — shows the 'нет уроков' note", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}/course-progress`, {
      studyGroupId: GID,
      courseId: COURSE_ID,
      passedLessons: 0,
      totalLessons: 0,
    });

    await page.goto(`/study-groups/${GID}`);

    await expect(page.getByText("В курсе группы пока нет уроков.")).toBeVisible();
  });

  test("section is hidden without Scheduling.Sessions.View", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      "Permissions.StudyGroups.StudyGroups.View",
      "Permissions.StudyGroups.Enrollments.View",
    ]);

    await page.goto(`/study-groups/${GID}`);

    await expect(
      page.getByRole("heading", { name: "Английский A1 · утро", level: 1 }),
    ).toBeVisible();
    await expect(page.getByText("Прогресс по программе")).toHaveCount(0);
  });
});
