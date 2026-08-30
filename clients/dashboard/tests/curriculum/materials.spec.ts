import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const CID = "cccccccc-cccc-cccc-cccc-cccccccccccc";
const MID = "dddddddd-dddd-dddd-dddd-dddddddddddd";
const LID = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee";
const SID = "11111111-1111-1111-1111-111111111111";

const TREE = [{ id: SID, name: "Английский язык", slug: "english", sortOrder: 0, children: [] }];

const DETAIL = {
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
  modules: [
    {
      id: MID,
      title: "Раздел 1",
      description: null,
      sortOrder: 0,
      lessons: [
        {
          id: LID,
          courseModuleId: MID,
          title: "Урок 1",
          objectives: null,
          content: null,
          durationMinutes: 45,
          sortOrder: 0,
        },
      ],
    },
  ],
};

const ALL_PERMS = [
  "Permissions.Curriculum.Courses.View",
  "Permissions.Curriculum.Courses.Update",
  "Permissions.Curriculum.Lessons.Update",
  "Permissions.Curriculum.LessonMaterials.View",
  "Permissions.Curriculum.LessonMaterials.Manage",
];

async function openMaterialForm(page: import("@playwright/test").Page) {
  await page.goto(`/courses/${CID}`);
  await page.getByRole("button", { name: "Развернуть урок" }).click();
  await page.getByRole("button", { name: "Материалы урока" }).click();
  await page.getByRole("button", { name: "Добавить материал" }).click();
}

test.describe("curriculum — lesson materials", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    await mockJsonResponse(page, "**/api/v1/subjects/tree", TREE);
    await mockJsonResponse(page, `**/api/v1/courses/${CID}`, DETAIL);
    await mockJsonResponse(page, `**/api/v1/lessons/${LID}/materials`, []);
  });

  test("file and link inputs are mutually exclusive", async ({ page }) => {
    await openMaterialForm(page);

    // Default mode is "link": URL field present, no file picker.
    await expect(page.getByLabel("URL")).toBeVisible();
    await expect(page.getByRole("button", { name: "Выбрать файл" })).toHaveCount(0);
    // Nothing entered yet → submit disabled (exactly one of file/link required).
    await expect(page.getByRole("button", { name: "Добавить", exact: true })).toBeDisabled();

    // Switch to "file" mode: URL field gone, file picker appears.
    await page.getByRole("button", { name: "Файл" }).click();
    await expect(page.getByLabel("URL")).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Выбрать файл" })).toBeVisible();
  });

  test("adding a link material posts url (and never fileId)", async ({ page }) => {
    await mockJsonResponse(
      page,
      `**/api/v1/lessons/${LID}/materials`,
      {
        id: "mat-1",
        lessonId: LID,
        kind: "Link",
        title: "Учебник",
        fileId: null,
        url: "https://example.com/textbook",
        visibleToStudents: true,
        sortOrder: 0,
      },
      { method: "POST" },
    );
    await openMaterialForm(page);

    await page.getByLabel("Название материала").fill("Учебник");
    await page.getByLabel("URL").fill("https://example.com/textbook");

    const post = page.waitForRequest(
      (r) => r.url().includes(`/lessons/${LID}/materials`) && r.method() === "POST",
    );
    await page.getByRole("button", { name: "Добавить", exact: true }).click();

    const req = await post;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      kind: "Link",
      title: "Учебник",
      url: "https://example.com/textbook",
      fileId: null,
      visibleToStudents: true,
    });
  });
});
