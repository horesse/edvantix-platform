import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";
import { PERMS, ROOM_ID, room } from "./fixtures";

test.describe("справочники расписания", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("/settings/rooms — создание аудитории", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.roomsView,
      PERMS.roomsManage,
    ]);
    await mockJsonResponse(page, "**/api/v1/rooms", [room(ROOM_ID, "Кабинет 1")]);

    await page.goto("/settings/rooms");
    await expect(page.getByRole("heading", { name: "Аудитории" })).toBeVisible();
    await expect(page.getByText("Кабинет 1").first()).toBeVisible();

    await page.route("**/api/v1/rooms", async (route) => {
      if (route.request().method() === "POST") {
        await route.fulfill({
          status: 200,
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify("11111111-1111-1111-1111-111111111111"),
        });
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify([room(ROOM_ID, "Кабинет 1")]),
      });
    });

    await page.getByRole("button", { name: "Новая аудитория" }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Название").fill("Онлайн-зал");

    const postReq = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/rooms") && r.method() === "POST",
    );
    await dialog.getByRole("button", { name: "Создать" }).click();
    const req = await postReq;
    expect(req.postDataJSON()).toMatchObject({ name: "Онлайн-зал", isVirtual: false });
  });

  test("/settings/non-working-days — добавление дня", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      PERMS.templatesView,
      PERMS.templatesManage,
    ]);
    await mockJsonResponse(page, "**/api/v1/non-working-days**", []);

    await page.goto("/settings/non-working-days");
    await expect(page.getByRole("heading", { name: "Нерабочие дни" })).toBeVisible();

    await mockJsonResponse(
      page,
      "**/api/v1/non-working-days",
      "22222222-2222-2222-2222-222222222222",
      { method: "POST" },
    );

    await page.getByRole("button", { name: "Добавить день" }).first().click();
    const dialog = page.getByRole("dialog");
    await dialog.getByLabel("Дата").fill("2027-01-01");
    await dialog.getByLabel("Описание").fill("Новый год");

    const postReq = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/non-working-days") && r.method() === "POST",
    );
    await dialog.getByRole("button", { name: "Добавить" }).click();
    const req = await postReq;
    expect(req.postDataJSON()).toEqual({ date: "2027-01-01", description: "Новый год" });
  });
});
