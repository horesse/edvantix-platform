import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const COURSE_ID = "c0000000-0000-0000-0000-000000000001";
const TEACHER_ID = "70000000-0000-0000-0000-000000000001";

function group(over: Record<string, unknown> = {}) {
  return {
    id: "a0000000-0000-0000-0000-000000000001",
    code: "ENG-A1",
    name: "Английский A1 · утро",
    courseId: COURSE_ID,
    primaryTeacherId: TEACHER_ID,
    format: "Offline",
    capacity: 8,
    activeEnrollmentCount: 3,
    startDate: "2026-02-01",
    endDate: null,
    status: "Forming",
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-01-10T00:00:00Z",
    ...over,
  };
}

const COURSES = paged([
  { id: COURSE_ID, subjectId: "s1", title: "Английский язык", slug: "english", description: null, level: "Beginner", durationHours: 40, status: "Published", coverFileId: null, publishedAtUtc: "2026-01-01T00:00:00Z", createdAtUtc: "2026-01-01T00:00:00Z" },
]);
const TEACHERS = paged([
  { id: TEACHER_ID, lastName: "Петрова", firstName: "Анна", middleName: null, displayName: "Петрова Анна", phone: "", email: "anna@acme.com", userId: null, status: "Active", bio: null, specializations: [], hourlyRate: null, avatarFileId: null },
]);

const VIEW = ["Permissions.StudyGroups.StudyGroups.View"];
const CREATE = [...VIEW, "Permissions.StudyGroups.StudyGroups.Create"];

test.describe("study-groups — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", VIEW);
    await mockJsonResponse(page, "**/api/v1/courses?**", COURSES);
    await mockJsonResponse(page, "**/api/v1/teachers?**", TEACHERS);
  });

  test("renders the heading and a group row", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
    await page.goto("/study-groups");

    await expect(
      page.getByRole("heading", { name: "Учебные группы", level: 1 }),
    ).toBeVisible();
    await expect(page.getByText("Английский A1 · утро").last()).toBeVisible();
    await expect(page.getByText("Набор").last()).toBeVisible();
  });

  test("empty state when there are no groups", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([]));
    await page.goto("/study-groups");
    await expect(
      page.getByRole("heading", { name: "Пока нет учебных групп", level: 2 }),
    ).toBeVisible();
  });

  test("status filter re-queries with ?status=Active", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
    await page.goto("/study-groups");
    await expect(page.getByText("Английский A1 · утро").last()).toBeVisible();

    const req = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/study-groups?") && r.url().includes("status=Active"),
    );
    await mockJsonResponse(
      page,
      "**/api/v1/study-groups?**",
      paged([group({ name: "Английский A1 · вечер", status: "Active" })]),
    );
    await page.getByRole("button", { name: "Идёт", exact: true }).click();
    await req;
    await expect(page.getByText("Английский A1 · вечер").last()).toBeVisible();
  });

  test("format filter re-queries with ?format=Online", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
    await page.goto("/study-groups");
    await expect(page.getByText("Английский A1 · утро").last()).toBeVisible();

    const req = page.waitForRequest((r) => r.url().includes("format=Online"));
    await page.getByRole("button", { name: "Формат" }).click();
    await page.getByRole("menuitemradio", { name: "Онлайн" }).click();
    await req;
  });

  test("course filter re-queries with ?courseId=", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
    await page.goto("/study-groups");
    await expect(page.getByText("Английский A1 · утро").last()).toBeVisible();

    const req = page.waitForRequest((r) => r.url().includes(`courseId=${COURSE_ID}`));
    await page.getByRole("button", { name: "Курс" }).click();
    await page.getByRole("menuitemradio", { name: "Английский язык" }).click();
    await req;
  });

  test("create button is gated by StudyGroups.Create", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
    await page.goto("/study-groups");
    await expect(page.getByRole("button", { name: /Новая группа/ })).toHaveCount(0);
  });

  test("create dialog posts the entered fields via mutate(arg)", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", CREATE);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([group()]));
    const sent = captureRequest(page, "**/api/v1/study-groups");
    await page.goto("/study-groups");

    await page.getByRole("button", { name: /Новая группа/ }).first().click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("Код").fill("ENG-B1-2026");
    await dialog.getByLabel("Название").fill("Английский B1");
    await dialog.getByLabel("Курс").click();
    await page.getByRole("menuitemradio", { name: "Английский язык" }).click();
    await dialog.getByLabel("Основной преподаватель").click();
    await page.getByRole("menuitemradio", { name: "Петрова Анна" }).click();
    await dialog.getByLabel("Вместимость").fill("10");
    await dialog.getByLabel("Дата старта").fill("2026-03-01");
    await dialog.getByRole("button", { name: /Создать/ }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      code: "ENG-B1-2026",
      name: "Английский B1",
      courseId: COURSE_ID,
      primaryTeacherId: TEACHER_ID,
      format: "Offline",
      capacity: 10,
      startDate: "2026-03-01",
    });
  });
});
