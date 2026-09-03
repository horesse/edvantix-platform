import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const STUDENTS = [
  {
    id: "00000000-0000-0000-0000-0000000000a1",
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
  },
  {
    id: "00000000-0000-0000-0000-0000000000a2",
    lastName: "Петрова",
    firstName: "Анна",
    middleName: null,
    displayName: "Петрова Анна",
    birthDate: "2011-03-12",
    phone: "+7 900 444-55-66",
    email: "anna@acme.com",
    userId: null,
    status: "Lead",
    source: null,
    avatarFileId: null,
    managerUserId: "u-test-1",
    enrolledAtUtc: "2025-10-01T00:00:00Z",
  },
];

const CREATE_PERMS = [
  "Permissions.People.Students.View",
  "Permissions.People.Students.Create",
];

test.describe("people/students — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    // Manager filter combobox loads the tenant user list on mount.
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([]));
    // EDX-018 duplicate check — default to "no duplicates"; specific tests override.
    await mockJsonResponse(page, "**/api/v1/people/duplicate-candidates**", []);
  });

  test("renders heading and a student row", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/students?**", paged(STUDENTS));
    await page.goto("/students");

    await expect(page.getByRole("heading", { name: "Ученики", level: 1 })).toBeVisible();
    await expect(page.getByText("Иванов Пётр Сергеевич").last()).toBeVisible();
    await expect(page.getByText("+7 900 111-22-33").last()).toBeVisible();
  });

  test("empty state when no students", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/students?**", paged([]));
    await page.goto("/students");

    await expect(
      page.getByRole("heading", { name: "Пока нет учеников", level: 2 }),
    ).toBeVisible();
  });

  test("search re-queries", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/students?**", paged(STUDENTS));
    await page.goto("/students");
    await expect(page.getByText("Петрова Анна").last()).toBeVisible();

    await mockJsonResponse(page, "**/api/v1/students?**", paged([STUDENTS[0]]));
    await page.getByPlaceholder(/Поиск по фамилии/).fill("иванов");

    await expect(page.getByText("Иванов Пётр Сергеевич").last()).toBeVisible();
    await expect(page.getByText("Петрова Анна")).toHaveCount(0);
  });

  test("create button hidden without the Create permission", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/students?**", paged(STUDENTS));
    await page.goto("/students");
    await expect(page.getByRole("button", { name: /Новый ученик/ })).toHaveCount(0);
  });

  test("duplicate warning shows and «Всё равно создать» still submits (EDX-018)", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", CREATE_PERMS);
    await mockJsonResponse(page, "**/api/v1/students?**", paged(STUDENTS));
    await mockJsonResponse(page, "**/api/v1/people/duplicate-candidates**", [
      {
        id: STUDENTS[0].id,
        personType: "Student",
        displayName: STUDENTS[0].displayName,
        phone: STUDENTS[0].phone,
        email: STUDENTS[0].email,
        phoneMatches: true,
        emailMatches: false,
      },
    ]);
    const sent = captureRequest(page, "**/api/v1/students");
    await page.goto("/students");

    await page.getByRole("button", { name: /Новый ученик/ }).first().click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("Фамилия").fill("Иванов");
    await dialog.getByLabel("Имя").fill("Пётр");
    await dialog.getByLabel("Дата рождения").fill("2012-01-15");
    await dialog.getByLabel("Телефон").fill("+7 900 111-22-33");
    await dialog.getByLabel("E-mail").fill("petya@acme.com");

    await expect(dialog.getByText(/Возможно, это дубль/)).toBeVisible();
    await expect(
      dialog.getByRole("link", { name: "Иванов Пётр Сергеевич" }),
    ).toBeVisible();

    const proceed = dialog.getByRole("button", { name: "Всё равно создать" });
    await expect(proceed).toBeVisible();
    await proceed.click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({ lastName: "Иванов", firstName: "Пётр" });
  });

  test("no duplicate warning when the check returns nothing", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", CREATE_PERMS);
    await mockJsonResponse(page, "**/api/v1/students?**", paged(STUDENTS));
    await mockJsonResponse(page, "**/api/v1/people/duplicate-candidates**", []);
    await page.goto("/students");

    await page.getByRole("button", { name: /Новый ученик/ }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Фамилия").fill("Сидоров");
    await dialog.getByLabel("Имя").fill("Иван");
    await dialog.getByLabel("Телефон").fill("+7 900 000-00-00");

    await expect(dialog.getByText(/Возможно, это дубль/)).toHaveCount(0);
    await expect(dialog.getByRole("button", { name: "Создать" })).toBeVisible();
  });

  test("create dialog posts the entered fields via mutate(arg)", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", CREATE_PERMS);
    await mockJsonResponse(page, "**/api/v1/students?**", paged(STUDENTS));
    const sent = captureRequest(page, "**/api/v1/students");
    await page.goto("/students");

    await page.getByRole("button", { name: /Новый ученик/ }).first().click();
    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();

    await dialog.getByLabel("Фамилия").fill("Сидоров");
    await dialog.getByLabel("Имя").fill("Иван");
    await dialog.getByLabel("Дата рождения").fill("2012-01-15");
    await dialog.getByLabel("Телефон").fill("+7 900 000-00-00");
    await dialog.getByLabel("E-mail").fill("sid@acme.com");
    await dialog.getByRole("button", { name: /Создать/ }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      lastName: "Сидоров",
      firstName: "Иван",
      birthDate: "2012-01-15",
      email: "sid@acme.com",
      managerUserId: "u-test-1",
    });
  });
});
