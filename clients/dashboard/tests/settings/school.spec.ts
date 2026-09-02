import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// Stage 7 — /school/settings: the school-wide time zone + currency editor over
// GET/PUT /api/v1/tenants/settings (Multitenancy). Invoice numbering is a stub;
// non-working-days / rooms are links out.

async function grant(page: Page, perms: readonly string[]): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

test.describe("school/settings", () => {
  test("GET renders the current time zone and currency", async ({ page }) => {
    await grant(page, ["Permissions.SchoolSettings.Manage"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "Europe/Moscow",
      currency: "RUB",
    });

    await page.goto("/school/settings");

    // Section headers.
    await expect(
      page.getByRole("heading", { name: "Регион и валюта" }),
    ).toBeVisible();
    await expect(page.getByRole("heading", { name: "Нумерация счетов" })).toBeVisible();

    // The two comboboxes show the server values.
    await expect(page.getByText("Europe/Moscow").last()).toBeVisible();
    await expect(page.getByText("RUB", { exact: true }).last()).toBeVisible();

    // Links out to the calendar sub-screens.
    await expect(page.getByRole("link", { name: "Нерабочие дни" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Аудитории" })).toBeVisible();
  });

  test("PUT sends the changed pair", async ({ page }) => {
    await grant(page, ["Permissions.SchoolSettings.Manage"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
    });
    // PUT handler — method-scoped so it doesn't shadow the GET mock above.
    await mockJsonResponse(page, "**/api/v1/tenants/settings", "", {
      method: "PUT",
      status: 204,
    });

    await page.goto("/school/settings");
    await expect(page.getByText("USD", { exact: true }).last()).toBeVisible();

    // Save is disabled until something changes.
    const save = page.getByRole("button", { name: "Сохранить" });
    await expect(save).toBeDisabled();

    // Change the currency to RUB.
    await page.getByLabel("Валюта").click();
    await page.getByRole("menuitemradio", { name: /^RUB/ }).first().click();

    await expect(save).toBeEnabled();

    const putReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/tenants/settings") && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await save.click();
    const req = await putReq;
    expect(req.postDataJSON()).toEqual({
      timeZoneId: "UTC",
      currency: "RUB",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
    });
  });

  test("EDX-015 — toggling «Ограничивать материалы» sends the flag", async ({ page }) => {
    await grant(page, ["Permissions.SchoolSettings.Manage"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
    });
    await mockJsonResponse(page, "**/api/v1/tenants/settings", "", {
      method: "PUT",
      status: 204,
    });

    await page.goto("/school/settings");

    await expect(
      page.getByRole("heading", { name: "Доступ к материалам" }),
    ).toBeVisible();

    const toggle = page.getByRole("switch", {
      name: "Ограничивать материалы при задолженности",
    });
    await expect(toggle).toHaveAttribute("aria-checked", "false");
    await toggle.click();
    await expect(toggle).toHaveAttribute("aria-checked", "true");

    const putReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/tenants/settings") && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: "Сохранить" }).click();
    const req = await putReq;
    expect(req.postDataJSON()).toEqual({
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: true,
      debtGraceDays: 7,
    });
  });

  test("read-only for a user without Manage", async ({ page }) => {
    await grant(page, ["Permissions.SchoolSettings.View"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
    });

    await page.goto("/school/settings");

    await expect(page.getByText(/Только просмотр/)).toBeVisible();
    await expect(page.getByRole("button", { name: "Сохранить" })).toHaveCount(0);
  });
});
