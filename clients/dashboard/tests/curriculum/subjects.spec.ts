import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const ENG = "11111111-1111-1111-1111-111111111111";
const MATH = "22222222-2222-2222-2222-222222222222";
const ENG_CHILD = "33333333-3333-3333-3333-333333333333";

const TREE = [
  {
    id: ENG,
    name: "Английский язык",
    slug: "english",
    sortOrder: 0,
    children: [
      { id: ENG_CHILD, name: "Грамматика", slug: "grammar", sortOrder: 0, children: [] },
    ],
  },
  { id: MATH, name: "Математика", slug: "math", sortOrder: 1, children: [] },
];

const ALL_PERMS = [
  "Permissions.Curriculum.Subjects.View",
  "Permissions.Curriculum.Subjects.Create",
  "Permissions.Curriculum.Subjects.Update",
  "Permissions.Curriculum.Subjects.Delete",
];

test.describe("curriculum/subjects — tree", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    await mockJsonResponse(page, "**/api/v1/subjects/tree", TREE);
  });

  test("renders the heading and every node", async ({ page }) => {
    await page.goto("/subjects");
    await expect(page.getByRole("heading", { name: "Направления", level: 1 })).toBeVisible();
    await expect(page.getByText("Английский язык")).toBeVisible();
    await expect(page.getByText("Математика")).toBeVisible();
    await expect(page.getByText("Грамматика")).toBeVisible();
  });

  test("reorder sends PUT /subjects/order with the parent + ordered ids", async ({ page }) => {
    const sent = captureRequest(page, "**/api/v1/subjects/order");
    await page.goto("/subjects");
    await expect(page.getByText("Математика")).toBeVisible();

    await page.getByRole("button", { name: "Ниже" }).first().click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      parentId: null,
      orderedSubjectIds: [MATH, ENG],
    });
  });

  test("inline create posts POST /subjects with parentId null for a root", async ({ page }) => {
    const sent = captureRequest(page, "**/api/v1/subjects");
    await page.goto("/subjects");

    await page.getByRole("button", { name: /Новое направление/ }).first().click();
    const input = page.getByLabel("Название направления");
    await input.fill("Физика");
    await page.getByRole("button", { name: "Добавить", exact: true }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({ name: "Физика", parentId: null });
  });

  test("rename posts PUT /subjects/{id}", async ({ page }) => {
    const put = page.waitForRequest(
      (r) => r.url().includes(`/api/v1/subjects/${ENG}`) && r.method() === "PUT",
    );
    await mockJsonResponse(page, `**/api/v1/subjects/${ENG}`, "", { method: "PUT" });
    await page.goto("/subjects");

    await page.getByRole("button", { name: "Переименовать" }).first().click();
    const input = page.getByLabel("Название направления");
    await input.fill("Английский");
    await page.getByRole("button", { name: "Сохранить" }).click();

    const req = await put;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({ name: "Английский" });
  });
});
