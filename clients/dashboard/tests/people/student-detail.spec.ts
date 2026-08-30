import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const ID = "00000000-0000-0000-0000-0000000000a1";

const DETAIL = {
  id: ID,
  lastName: "Иванов",
  firstName: "Пётр",
  middleName: "Сергеевич",
  displayName: "Иванов Пётр Сергеевич",
  birthDate: "2010-05-01",
  phone: "+7 900 111-22-33",
  email: "petya@acme.com",
  userId: null,
  status: "Active",
  source: "Сайт",
  avatarFileId: null,
  managerUserId: "u-test-1",
  enrolledAtUtc: "2025-09-01T00:00:00Z",
  createdAtUtc: "2025-09-01T00:00:00Z",
  updatedAtUtc: null,
  guardianCount: 1,
  noteCount: 1,
};

const GUARDIAN_LINK = {
  id: "link-1",
  studentId: ID,
  guardianId: "g-1",
  relation: "мать",
  isPrimaryPayer: true,
  guardian: {
    id: "g-1",
    lastName: "Иванова",
    firstName: "Мария",
    displayName: "Иванова Мария",
    phone: "+7 900 999-88-77",
    email: "maria@acme.com",
    userId: null,
  },
};

const NOTE = {
  id: "note-1",
  studentId: ID,
  text: "Перевёлся из другой школы",
  authorUserId: "u-test-1",
  createdAtUtc: "2025-09-02T10:00:00Z",
};

const ALL_PERMS = [
  "Permissions.People.Students.View",
  "Permissions.People.Students.Update",
  "Permissions.People.Students.Delete",
  "Permissions.People.Students.ViewNotes",
];

test.describe("people/students/:id — detail", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", {
      items: [],
      pageNumber: 1,
      pageSize: 100,
      totalCount: 0,
      totalPages: 1,
      hasPrevious: false,
      hasNext: false,
    });
    await mockJsonResponse(page, `**/api/v1/students/${ID}/guardians`, [GUARDIAN_LINK]);
    await mockJsonResponse(page, `**/api/v1/students/${ID}/notes`, [NOTE]);
    await mockJsonResponse(page, `**/api/v1/students/${ID}`, DETAIL);
  });

  test("loads the student and shows stats", async ({ page }) => {
    await page.goto(`/students/${ID}`);
    await expect(
      page.getByRole("heading", { name: "Иванов Пётр Сергеевич", level: 1 }),
    ).toBeVisible();
    await expect(page.getByRole("link", { name: /К списку учеников/ })).toBeVisible();
  });

  test("guardians tab lists the linked guardian and payer badge", async ({ page }) => {
    await page.goto(`/students/${ID}`);
    await page.getByRole("button", { name: "Представители" }).click();
    await expect(page.getByText("Иванова Мария")).toBeVisible();
    await expect(page.getByText("Плательщик")).toBeVisible();
  });

  test("removing a guardian calls DELETE with the guardian id", async ({ page }) => {
    const del = captureRequest(page, `**/api/v1/students/${ID}/guardians/g-1`);
    await page.goto(`/students/${ID}`);
    await page.getByRole("button", { name: "Представители" }).click();
    await page.getByRole("button", { name: "Отвязать" }).click();
    await del.value();
  });

  test("notes tab shows the note and posts a new one", async ({ page }) => {
    // Method-scoped so the notes GET still falls through to the [NOTE] mock.
    await mockJsonResponse(page, `**/api/v1/students/${ID}/notes`, "note-2", {
      method: "POST",
    });
    await page.goto(`/students/${ID}`);
    await page.getByRole("button", { name: "Заметки" }).click();
    await expect(page.getByText("Перевёлся из другой школы")).toBeVisible();

    const post = page.waitForRequest(
      (r) => r.url().includes(`/students/${ID}/notes`) && r.method() === "POST",
    );
    await page.getByPlaceholder("Новая заметка…").fill("Новый комментарий");
    await page.getByRole("button", { name: /Добавить/ }).click();

    const req = await post;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({ text: "Новый комментарий" });
  });

  test("archive posts to the archive endpoint", async ({ page }) => {
    const archive = captureRequest(page, `**/api/v1/students/${ID}/archive`);
    await page.goto(`/students/${ID}`);
    await page.getByRole("button", { name: "В архив" }).click();
    await archive.value();
  });
});
