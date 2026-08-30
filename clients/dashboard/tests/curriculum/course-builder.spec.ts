import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const CID = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const MID = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const LID = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
const SID = "11111111-1111-1111-1111-111111111111";

const TREE = [{ id: SID, name: "Английский язык", slug: "english", sortOrder: 0, children: [] }];

type Over = Record<string, unknown>;

function lesson(over: Over = {}) {
  return {
    id: LID,
    courseModuleId: MID,
    title: "Урок 1",
    objectives: null,
    content: null,
    durationMinutes: 45,
    sortOrder: 0,
    ...over,
  };
}

function moduleWith(lessons: unknown[] = [], over: Over = {}) {
  return { id: MID, title: "Раздел 1", description: null, sortOrder: 0, lessons, ...over };
}

function detail(over: Over = {}) {
  return {
    id: CID,
    subjectId: SID,
    title: "Английский A1",
    slug: "anglijskij-a1",
    description: null,
    level: "Beginner",
    durationHours: 40,
    status: "Draft",
    coverFileId: null,
    publishedAtUtc: null,
    createdAtUtc: "2026-01-10T00:00:00Z",
    modules: [],
    ...over,
  };
}

const ALL_PERMS = [
  "Permissions.Curriculum.Courses.View",
  "Permissions.Curriculum.Courses.Update",
  "Permissions.Curriculum.Courses.Publish",
  "Permissions.Curriculum.Courses.Create",
  "Permissions.Curriculum.Courses.Delete",
  "Permissions.Curriculum.Lessons.Create",
  "Permissions.Curriculum.Lessons.Update",
  "Permissions.Curriculum.Lessons.Delete",
  "Permissions.Curriculum.LessonMaterials.View",
  "Permissions.Curriculum.LessonMaterials.Manage",
];

test.describe("curriculum/courses/:id — course builder", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    await mockJsonResponse(page, "**/api/v1/subjects/tree", TREE);
  });

  test("loads CourseDetailDto — title, status, stats", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/courses/${CID}`, detail({ modules: [moduleWith([lesson()])] }));
    await page.goto(`/courses/${CID}`);

    await expect(page.getByRole("heading", { name: "Английский A1", level: 1 })).toBeVisible();
    await expect(page.getByText("Черновик").last()).toBeVisible();
    await expect(page.getByText("разделов")).toBeVisible();
    await expect(page.getByText("Раздел 1")).toBeVisible();
  });

  test("adds a section via POST /courses/{id}/modules", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/courses/${CID}`, detail());
    await mockJsonResponse(page, `**/api/v1/courses/${CID}/modules`, "new-mid", { method: "POST" });
    await page.goto(`/courses/${CID}`);

    const post = page.waitForRequest(
      (r) => r.url().includes(`/courses/${CID}/modules`) && r.method() === "POST",
    );
    await page.getByRole("button", { name: "Добавить раздел" }).click();
    await page.getByLabel("Название раздела").fill("Вводный раздел");
    await page.getByRole("button", { name: "Добавить", exact: true }).click();

    const req = await post;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      title: "Вводный раздел",
      description: null,
    });
  });

  test("adds a lesson via POST /modules/{id}/lessons", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/courses/${CID}`, detail({ modules: [moduleWith([])] }));
    await mockJsonResponse(page, `**/api/v1/modules/${MID}/lessons`, "new-lid", { method: "POST" });
    await page.goto(`/courses/${CID}`);

    const post = page.waitForRequest(
      (r) => r.url().includes(`/modules/${MID}/lessons`) && r.method() === "POST",
    );
    await page.getByRole("button", { name: "Добавить урок" }).click();
    await page.getByLabel("Название урока").fill("Приветствие");
    await page.getByRole("button", { name: "Добавить", exact: true }).click();

    const req = await post;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      title: "Приветствие",
      objectives: null,
      content: null,
      durationMinutes: 45,
    });
  });

  test("editing a lesson auto-saves via PUT /lessons/{id} with the payload in mutate(arg)", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/courses/${CID}`,
      detail({ modules: [moduleWith([lesson()])] }),
    );
    await mockJsonResponse(page, `**/api/v1/lessons/${LID}`, "", { method: "PUT", status: 204 });
    await page.goto(`/courses/${CID}`);

    await page.getByRole("button", { name: "Развернуть урок" }).click();

    const put = page.waitForRequest(
      (r) => r.url().includes(`/api/v1/lessons/${LID}`) && r.method() === "PUT",
    );
    await page.getByLabel("Название урока").fill("Урок 1 — знакомство");

    const req = await put;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      title: "Урок 1 — знакомство",
      objectives: null,
      content: null,
      durationMinutes: 45,
    });
    await expect(page.getByText("Сохранено")).toBeVisible();
  });

  test("publish surfaces the 409 reason instead of swallowing it", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/courses/${CID}`, detail({ modules: [] }));
    await mockProblemDetails(page, `**/api/v1/courses/${CID}/publish`, 409, {
      title: "Конфликт",
      detail: "Курс должен содержать хотя бы один раздел.",
    });
    await page.goto(`/courses/${CID}`);

    await page.getByRole("button", { name: "Опубликовать" }).click();

    await expect(
      page.getByText("Курс должен содержать хотя бы один раздел").first(),
    ).toBeVisible();
  });
});
