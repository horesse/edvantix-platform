import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const GID = "a0000000-0000-0000-0000-000000000001";
const G2_ID = "a0000000-0000-0000-0000-000000000002";
const COURSE_ID = "c0000000-0000-0000-0000-000000000001";
const TEACHER_ID = "70000000-0000-0000-0000-000000000001";
const STU_A = "50000000-0000-0000-0000-0000000000a1";
const STU_B = "50000000-0000-0000-0000-0000000000b2";
const ENR_A = "e0000000-0000-0000-0000-0000000000a1";

type Over = Record<string, unknown>;

function enrollment(over: Over = {}) {
  return {
    id: ENR_A,
    studyGroupId: GID,
    studentId: STU_A,
    enrolledOn: "2026-02-01",
    leftOn: null,
    status: "Active",
    leaveReason: null,
    tariffId: null,
    discountPercent: 0,
    ...over,
  };
}

function detail(over: Over = {}) {
  return {
    id: GID,
    code: "ENG-A1",
    name: "Английский A1 · утро",
    courseId: COURSE_ID,
    primaryTeacherId: TEACHER_ID,
    format: "Offline",
    capacity: 2,
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

const STUDENTS = paged([
  { id: STU_A, lastName: "Смирнов", firstName: "Иван", middleName: null, displayName: "Смирнов Иван", birthDate: "2011-01-01", phone: "", email: "ivan@acme.com", userId: null, status: "Active", source: null, avatarFileId: null, managerUserId: "u-test-1", enrolledAtUtc: "2026-01-01T00:00:00Z" },
  { id: STU_B, lastName: "Кузнецова", firstName: "Ольга", middleName: null, displayName: "Кузнецова Ольга", birthDate: "2011-02-02", phone: "", email: "olga@acme.com", userId: null, status: "Active", source: null, avatarFileId: null, managerUserId: "u-test-1", enrolledAtUtc: "2026-01-01T00:00:00Z" },
]);
const TEACHERS = paged([
  { id: TEACHER_ID, lastName: "Петрова", firstName: "Анна", middleName: null, displayName: "Петрова Анна", phone: "", email: "anna@acme.com", userId: null, status: "Active", bio: null, specializations: [], hourlyRate: null, avatarFileId: null },
]);
const COURSES = paged([
  { id: COURSE_ID, subjectId: "s1", title: "Английский язык", slug: "english", description: null, level: "Beginner", durationHours: 40, status: "Published", coverFileId: null, publishedAtUtc: "2026-01-01T00:00:00Z", createdAtUtc: "2026-01-01T00:00:00Z" },
]);

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

test.describe("study-groups/:id — enrollments", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    await mockJsonResponse(page, "**/api/v1/students?**", STUDENTS);
    await mockJsonResponse(page, "**/api/v1/teachers?**", TEACHERS);
    await mockJsonResponse(page, "**/api/v1/courses?**", COURSES);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([detail(), detail({ id: G2_ID, code: "ENG-A2", name: "Английский A2" })]));
  });

  test("enrolls a selected student via POST with studentIds in mutate(arg)", async ({
    page,
  }) => {
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}`, detail());
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}/enrollments`,
      [ENR_A],
      { method: "POST" },
    );
    await page.goto(`/study-groups/${GID}`);
    await expect(page.getByRole("heading", { name: "Английский A1 · утро", level: 1 })).toBeVisible();

    const post = page.waitForRequest(
      (r) => r.url().includes(`/study-groups/${GID}/enrollments`) && r.method() === "POST",
    );
    await page.getByRole("button", { name: "Зачислить", exact: true }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Ученик").click();
    await page.getByRole("menuitemradio", { name: "Смирнов Иван" }).click();
    await dialog.getByRole("button", { name: /Зачислить \(1\)/ }).click();

    const req = await post;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      studentIds: [STU_A],
      discountPercent: 0,
    });
  });

  test("enroll surfaces the 409 'мест нет' instead of swallowing it", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/study-groups/${GID}`, detail());
    await mockProblemDetails(
      page,
      `**/api/v1/study-groups/${GID}/enrollments`,
      409,
      { title: "Конфликт", detail: "Мест нет — вместимость группы исчерпана." },
    );
    await page.goto(`/study-groups/${GID}`);

    await page.getByRole("button", { name: "Зачислить", exact: true }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Ученик").click();
    await page.getByRole("menuitemradio", { name: "Смирнов Иван" }).click();
    await dialog.getByRole("button", { name: /Зачислить \(1\)/ }).click();

    await expect(
      page.getByText("Мест нет — вместимость группы исчерпана.").last(),
    ).toBeVisible();
  });

  test("unenroll moves the row to 'Ушёл' — the row is not removed", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}`,
      detail({ status: "Active", activeEnrollmentCount: 1, enrollments: [enrollment()] }),
    );
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}/enrollments/${ENR_A}**`,
      "",
      { method: "DELETE", status: 204 },
    );
    await page.goto(`/study-groups/${GID}`);
    await expect(page.getByText("Смирнов Иван").last()).toBeVisible();

    const del = page.waitForRequest(
      (r) =>
        r.url().includes(`/study-groups/${GID}/enrollments/${ENR_A}`) &&
        r.method() === "DELETE",
    );
    await page.getByRole("button", { name: /Отчислить Смирнов Иван/ }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Причина").fill("Переезд");
    // Re-mock the detail GET so the post-mutation refetch returns status Left.
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}`,
      detail({
        status: "Active",
        activeEnrollmentCount: 0,
        enrollments: [enrollment({ status: "Left", leftOn: "2026-03-01", leaveReason: "Переезд" })],
      }),
    );
    await dialog.getByRole("button", { name: "Отчислить", exact: true }).click();

    await del;
    await expect(page.getByText("Смирнов Иван").last()).toBeVisible();
    await expect(page.getByText("Ушёл").last()).toBeVisible();
  });

  test("transfer posts targetStudyGroupId via mutate(arg)", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/study-groups/${GID}`,
      detail({ status: "Active", activeEnrollmentCount: 1, enrollments: [enrollment()] }),
    );
    await mockJsonResponse(
      page,
      `**/api/v1/enrollments/${ENR_A}/transfer`,
      "new-enrollment-id",
      { method: "POST" },
    );
    await page.goto(`/study-groups/${GID}`);
    await expect(page.getByText("Смирнов Иван").last()).toBeVisible();

    const post = page.waitForRequest(
      (r) => r.url().includes(`/enrollments/${ENR_A}/transfer`) && r.method() === "POST",
    );
    await page.getByRole("button", { name: /Перевести Смирнов Иван/ }).click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Целевая группа").click();
    await page.getByRole("menuitemradio", { name: /ENG-A2/ }).click();
    await dialog.getByRole("button", { name: "Перевести", exact: true }).click();

    const req = await post;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      targetStudyGroupId: G2_ID,
      transferDate: null,
    });
  });
});
