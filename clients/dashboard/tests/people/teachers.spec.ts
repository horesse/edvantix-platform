import { expect, test } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const TEACHERS = [
  {
    id: "00000000-0000-0000-0000-0000000000t1",
    lastName: "Смирнов",
    firstName: "Олег",
    middleName: null,
    displayName: "Смирнов Олег",
    phone: "+7 900 111-00-11",
    email: "oleg@acme.com",
    userId: null,
    status: "Active",
    bio: null,
    specializations: ["Математика", "Физика"],
    hourlyRate: 1500,
    avatarFileId: null,
  },
];

const ALL_PERMS = [
  "Permissions.People.Teachers.View",
  "Permissions.People.Teachers.Create",
  "Permissions.People.Teachers.Update",
  "Permissions.People.Teachers.Delete",
];

test.describe("people/teachers", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
  });

  test("list renders a teacher row with specializations", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/teachers?**", paged(TEACHERS));
    await page.goto("/teachers");

    await expect(page.getByRole("heading", { name: "Преподаватели", level: 1 })).toBeVisible();
    await expect(page.getByText("Смирнов Олег").last()).toBeVisible();
    await expect(page.getByText("Математика, Физика").last()).toBeVisible();
  });

  test("create dialog posts specializations split on comma", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/teachers?**", paged(TEACHERS));
    const sent = captureRequest(page, "**/api/v1/teachers");
    await page.goto("/teachers");

    await page.getByRole("button", { name: /Новый преподаватель/ }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Фамилия").fill("Кузнецов");
    await dialog.getByLabel("Имя").fill("Дмитрий");
    await dialog.getByLabel("Телефон").fill("+7 900 222-33-44");
    await dialog.getByLabel("E-mail").fill("dk@acme.com");
    await dialog.getByLabel("Специализации").fill("Химия, Биология");
    await dialog.getByRole("button", { name: /Создать/ }).click();

    const body = (await sent.value()).body as Record<string, unknown>;
    expect(body).toMatchObject({
      lastName: "Кузнецов",
      firstName: "Дмитрий",
      specializations: ["Химия", "Биология"],
    });
  });

  test("detail: deactivate posts to the deactivate endpoint", async ({ page }) => {
    const id = TEACHERS[0].id;
    const deactivate = captureRequest(page, `**/api/v1/teachers/${id}/deactivate`);
    await mockJsonResponse(page, `**/api/v1/teachers/${id}`, TEACHERS[0]);
    await page.goto(`/teachers/${id}`);

    await expect(page.getByRole("heading", { name: "Смирнов Олег", level: 1 })).toBeVisible();
    await page.getByRole("button", { name: "Деактивировать" }).click();
    await deactivate.value();
  });
});
