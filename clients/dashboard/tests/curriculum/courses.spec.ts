import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const SUBJECT_ID = "11111111-1111-1111-1111-111111111111";
const TREE = [
  { id: SUBJECT_ID, name: "Английский язык", slug: "english", sortOrder: 0, children: [] },
];

function course(over: Record<string, unknown> = {}) {
  return {
    id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    subjectId: SUBJECT_ID,
    title: "Английский A1",
    slug: "anglijskij-a1",
    description: null,
    level: "Beginner",
    durationHours: 40,
    status: "Draft",
    coverFileId: null,
    publishedAtUtc: null,
    createdAtUtc: "2026-01-10T00:00:00Z",
    ...over,
  };
}

const VIEW = ["Permissions.Curriculum.Courses.View"];
const CREATE = [...VIEW, "Permissions.Curriculum.Courses.Create"];

test.describe("curriculum/courses — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", VIEW);
    await mockJsonResponse(page, "**/api/v1/subjects/tree", TREE);
  });

  test("renders the heading and a course row", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([course()]));
    await page.goto("/courses");

    await expect(page.getByRole("heading", { name: "Курсы", level: 1 })).toBeVisible();
    await expect(page.getByText("Английский A1").last()).toBeVisible();
  });

  test("empty state when there are no courses", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([]));
    await page.goto("/courses");
    await expect(page.getByRole("heading", { name: "Пока нет курсов", level: 2 })).toBeVisible();
  });

  test("status filter re-queries with ?status=Published", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([course()]));
    await page.goto("/courses");
    await expect(page.getByText("Английский A1").last()).toBeVisible();

    const req = page.waitForRequest(
      (r) => r.url().includes("/api/v1/courses?") && r.url().includes("status=Published"),
    );
    await mockJsonResponse(
      page,
      "**/api/v1/courses?**",
      paged([course({ title: "Английский B1", status: "Published" })]),
    );
    await page.getByRole("button", { name: "Опубликованы" }).click();
    await req;
    await expect(page.getByText("Английский B1").last()).toBeVisible();
  });

  test("level filter re-queries with ?level=Intermediate", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([course()]));
    await page.goto("/courses");
    await expect(page.getByText("Английский A1").last()).toBeVisible();

    const req = page.waitForRequest((r) => r.url().includes("level=Intermediate"));
    await page.getByRole("button", { name: "Уровень" }).click();
    await page.getByRole("menuitemradio", { name: "Средний" }).click();
    await req;
  });

  test("trash link is gated by ViewTrash", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([course()]));
    await page.goto("/courses");
    await expect(page.getByRole("link", { name: /Корзина/ })).toHaveCount(0);
  });

  test("create dialog posts the entered fields via mutate(arg)", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", CREATE);
    await mockJsonResponse(page, "**/api/v1/courses?**", paged([course()]));
    const sent = captureRequest(page, "**/api/v1/courses");
    await page.goto("/courses");

    await page.getByRole("button", { name: /Новый курс/ }).first().click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("Название").fill("Английский A2");
    await dialog.getByLabel("Направление").click();
    await page.getByRole("menuitemradio", { name: "Английский язык" }).click();
    await dialog.getByLabel("Длительность, часов").fill("30");
    await dialog.getByRole("button", { name: /Создать/ }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      subjectId: SUBJECT_ID,
      title: "Английский A2",
      level: "Beginner",
      durationHours: 30,
    });
  });
});
