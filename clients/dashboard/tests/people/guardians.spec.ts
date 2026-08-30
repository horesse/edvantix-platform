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
];

test.describe("people/guardians", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ALL_PERMS);
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

  test("detail loads and shows the account section", async ({ page }) => {
    const id = GUARDIANS[0].id;
    await mockJsonResponse(page, `**/api/v1/guardians/${id}`, GUARDIANS[0]);
    await page.goto(`/guardians/${id}`);

    await expect(page.getByRole("heading", { name: "Иванова Мария", level: 1 })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Учётная запись" })).toBeVisible();
  });
});
