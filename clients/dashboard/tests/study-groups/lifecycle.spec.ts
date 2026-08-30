import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const GID = "a0000000-0000-0000-0000-000000000001";
const COURSE_ID = "c0000000-0000-0000-0000-000000000001";
const TEACHER_ID = "70000000-0000-0000-0000-000000000001";

type Over = Record<string, unknown>;

function detail(over: Over = {}) {
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
    status: "Forming",
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-01-10T00:00:00Z",
    enrollments: [],
    teachers: [],
    ...over,
  };
}

const ALL_PERMS = [
  "Permissions.StudyGroups.StudyGroups.View",
  "Permissions.StudyGroups.StudyGroups.Update",
  "Permissions.StudyGroups.StudyGroups.Archive",
  "Permissions.StudyGroups.StudyGroups.Delete",
  "Permissions.StudyGroups.Enrollments.View",
  "Permissions.StudyGroups.Enrollments.Create",
  "Permissions.StudyGroups.Enrollments.Delete",
  "Permissions.StudyGroups.Enrollments.Transfer",
];

test.describe("study-groups/:id — lifecycle", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    await mockJsonResponse(page, "**/api/v1/students?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/teachers?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([]));
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([]));
  });

  test("activate surfaces the 409 reason when the group has no enrollments", async ({
    page,
  }) => {
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}`, detail());
    await mockProblemDetails(page, `**/api/v1/study-groups/${GID}/activate`, 409, {
      title: "Конфликт",
      detail: "В группе нет ни одного зачисления — активировать нельзя.",
    });
    await page.goto(`/study-groups/${GID}`);

    await page.getByRole("button", { name: "Активировать" }).click();

    await expect(
      page.getByText("В группе нет ни одного зачисления — активировать нельзя.").first(),
    ).toBeVisible();
  });

  test("read-only after Finished — no lifecycle or roster actions", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}`,
      detail({ status: "Finished" }),
    );
    await page.goto(`/study-groups/${GID}`);
    await expect(
      page.getByRole("heading", { name: "Английский A1 · утро", level: 1 }),
    ).toBeVisible();

    await expect(page.getByText("Завершена").last()).toBeVisible();
    await expect(
      page.getByText("карточка и состав доступны только для чтения"),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Изменить" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Активировать" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Завершить" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Отменить" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Зачислить" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Добавить" })).toHaveCount(0);
  });

  test("read-only after Cancelled — enroll is blocked", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}`,
      detail({ status: "Cancelled" }),
    );
    await page.goto(`/study-groups/${GID}`);

    await expect(page.getByText("Отменена").last()).toBeVisible();
    await expect(
      page.getByText("карточка и состав доступны только для чтения"),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "Зачислить" })).toHaveCount(0);
  });

  test("activate is sent when the group has an enrollment", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}`,
      detail({
        activeEnrollmentCount: 1,
        enrollments: [
          {
            id: "e1",
            studyGroupId: GID,
            studentId: "stu-1",
            enrolledOn: "2026-02-01",
            leftOn: null,
            status: "Active",
            leaveReason: null,
            tariffId: null,
            discountPercent: 0,
          },
        ],
      }),
    );
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}/activate`, "", {
      method: "POST",
      status: 204,
    });
    await page.goto(`/study-groups/${GID}`);

    const post = page.waitForRequest(
      (r) => r.url().includes(`/study-groups/${GID}/activate`) && r.method() === "POST",
    );
    await page.getByRole("button", { name: "Активировать" }).click();
    await post;
  });
});
