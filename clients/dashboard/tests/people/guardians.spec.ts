import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const GUARDIANS = [
  {
    id: "00000000-0000-0000-0000-0000000000g1",
    lastName: "Иванова",
    firstName: "Мария",
    displayName: "Иванова Мария",
    phone: "+7 900 999-88-77",
    email: "maria@acme.com",
    userId: null,
  },
];

const ALL_PERMS = [
  "Permissions.People.Guardians.View",
  "Permissions.People.Guardians.Create",
  "Permissions.People.Guardians.Update",
  "Permissions.People.Guardians.Delete",
  "Permissions.People.Students.View",
];

const WARD_LINKS = [
  {
    id: "00000000-0000-0000-0000-0000000000l1",
    studentId: "00000000-0000-0000-0000-0000000000s1",
    guardianId: GUARDIANS[0].id,
    relation: "мать",
    isPrimaryPayer: true,
    student: {
      id: "00000000-0000-0000-0000-0000000000s1",
      lastName: "Иванов",
      firstName: "Пётр",
      middleName: null,
      displayName: "Иванов Пётр",
      birthDate: "2012-05-01",
      phone: "+7 900 111-22-33",
      email: "petr@acme.com",
      userId: null,
      status: "Active",
      source: null,
      avatarFileId: null,
      managerUserId: "00000000-0000-0000-0000-0000000000m1",
      enrolledAtUtc: "2024-09-01T00:00:00Z",
    },
  },
];

test.describe("people/guardians", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
    // EDX-018 duplicate check — default to "no duplicates"; specific tests override.
    await mockJsonResponse(page, "**/api/v1/people/duplicate-candidates**", []);
  });

  test("list renders a guardian row", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/guardians?**", paged(GUARDIANS));
    await page.goto("/guardians");

    await expect(page.getByRole("heading", { name: "Представители", level: 1 })).toBeVisible();
    await expect(page.getByText("Иванова Мария").last()).toBeVisible();
  });

  test("create dialog posts the guardian fields", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/guardians?**", paged(GUARDIANS));
    const sent = captureRequest(page, "**/api/v1/guardians");
    await page.goto("/guardians");

    await page.getByRole("button", { name: /Новый представитель/ }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Фамилия").fill("Петров");
    await dialog.getByLabel("Имя").fill("Сергей");
    await dialog.getByLabel("Телефон").fill("+7 900 000-11-22");
    await dialog.getByLabel("E-mail").fill("sp@acme.com");
    await dialog.getByRole("button", { name: /Создать/ }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      lastName: "Петров",
      firstName: "Сергей",
      email: "sp@acme.com",
    });
  });

  test("duplicate warning shows in the create dialog (EDX-018)", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/guardians?**", paged(GUARDIANS));
    await mockJsonResponse(page, "**/api/v1/people/duplicate-candidates**", [
      {
        id: GUARDIANS[0].id,
        personType: "Guardian",
        displayName: GUARDIANS[0].displayName,
        phone: GUARDIANS[0].phone,
        email: GUARDIANS[0].email,
        phoneMatches: false,
        emailMatches: true,
      },
    ]);
    await page.goto("/guardians");

    await page.getByRole("button", { name: /Новый представитель/ }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Фамилия").fill("Иванова");
    await dialog.getByLabel("Имя").fill("Мария");
    await dialog.getByLabel("E-mail").fill("maria@acme.com");

    await expect(dialog.getByText(/Возможно, это дубль/)).toBeVisible();
    await expect(
      dialog.getByRole("button", { name: "Всё равно создать" }),
    ).toBeVisible();
  });

  test("detail loads and shows the account section", async ({ page }) => {
    const id = GUARDIANS[0].id;
    await mockJsonResponse(page, `**/api/v1/guardians/${id}`, GUARDIANS[0]);
    await mockJsonResponse(page, `**/api/v1/guardians/${id}/students`, []);
    await page.goto(`/guardians/${id}`);

    await expect(page.getByRole("heading", { name: "Иванова Мария", level: 1 })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Учётная запись" })).toBeVisible();
  });

  test("wards block lists the guardian's students", async ({ page }) => {
    const id = GUARDIANS[0].id;
    await mockJsonResponse(page, `**/api/v1/guardians/${id}`, GUARDIANS[0]);
    await mockJsonResponse(page, `**/api/v1/guardians/${id}/students`, WARD_LINKS);
    await page.goto(`/guardians/${id}`);

    await expect(page.getByRole("heading", { name: "Подопечные" })).toBeVisible();
    const row = page.getByRole("link", { name: /Иванов Пётр/ });
    await expect(row).toBeVisible();
    await expect(row).toHaveAttribute("href", `/students/${WARD_LINKS[0].studentId}`);
    await expect(page.getByText("Плательщик").last()).toBeVisible();
  });

  test("wards block shows an empty state when there are no links", async ({ page }) => {
    const id = GUARDIANS[0].id;
    await mockJsonResponse(page, `**/api/v1/guardians/${id}`, GUARDIANS[0]);
    await mockJsonResponse(page, `**/api/v1/guardians/${id}/students`, []);
    await page.goto(`/guardians/${id}`);

    await expect(
      page.getByText("К этому представителю не привязан ни один ученик."),
    ).toBeVisible();
  });
});
