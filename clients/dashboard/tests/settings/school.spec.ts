import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

// Stage 7 — /school/settings: the school-wide time zone + currency editor over
// GET/PUT /api/v1/tenants/settings (Multitenancy). EDX-014 added the invoice-
// number template editor; non-working-days / rooms are links out.

const DEFAULT_TEMPLATE = "{YYYY}-{NNNN}";

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
      invoiceNumberTemplate: DEFAULT_TEMPLATE,
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
      invoiceNumberTemplate: DEFAULT_TEMPLATE,
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

  test("EDX-014 — shows the server template and previews the next number", async ({
    page,
  }) => {
    await grant(page, ["Permissions.SchoolSettings.Manage"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
      invoiceNumberTemplate: "INV-{YY}/{MM}/{NNN}",
    });

    await page.goto("/school/settings");

    const input = page.getByLabel("Шаблон номера");
    await expect(input).toHaveValue("INV-{YY}/{MM}/{NNN}");

    const now = new Date();
    const yy = String(now.getUTCFullYear() % 100).padStart(2, "0");
    const mm = String(now.getUTCMonth() + 1).padStart(2, "0");
    await expect(
      page.getByText(`INV-${yy}/${mm}/001`, { exact: false }),
    ).toBeVisible();
    // Year token → per-year reset wording.
    await expect(page.getByText(/обнуляется в начале каждого/)).toBeVisible();
  });

  test("EDX-014 — editing the template sends invoiceNumberTemplate", async ({
    page,
  }) => {
    await grant(page, ["Permissions.SchoolSettings.Manage"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
      invoiceNumberTemplate: DEFAULT_TEMPLATE,
    });
    await mockJsonResponse(page, "**/api/v1/tenants/settings", "", {
      method: "PUT",
      status: 204,
    });

    await page.goto("/school/settings");

    const input = page.getByLabel("Шаблон номера");
    await input.fill("{YYYY}/{NNNNNN}");

    const save = page.getByRole("button", { name: "Сохранить" });
    await expect(save).toBeEnabled();

    const putReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/tenants/settings") && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await save.click();
    const req = await putReq;
    expect(req.postDataJSON()).toEqual({
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
      invoiceNumberTemplate: "{YYYY}/{NNNNNN}",
    });
  });

  test("EDX-014 — an invalid template blocks Save", async ({ page }) => {
    await grant(page, ["Permissions.SchoolSettings.Manage"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
      invoiceNumberTemplate: DEFAULT_TEMPLATE,
    });

    await page.goto("/school/settings");

    const input = page.getByLabel("Шаблон номера");
    // No {N…} counter → invalid.
    await input.fill("{YYYY}-INV");
    await expect(page.getByRole("alert")).toBeVisible();
    await expect(page.getByRole("button", { name: "Сохранить" })).toBeDisabled();

    // A stray brace → still invalid.
    await input.fill("{YYYY}-{NN");
    await expect(page.getByRole("button", { name: "Сохранить" })).toBeDisabled();

    // Fix it → Save unlocks.
    await input.fill("{YYYY}-{NNNN}-X");
    await expect(page.getByRole("button", { name: "Сохранить" })).toBeEnabled();
  });

  test("EDX-014 — template input is read-only without Manage", async ({ page }) => {
    await grant(page, ["Permissions.SchoolSettings.View"]);
    await mockJsonResponse(page, "**/api/v1/tenants/settings", {
      timeZoneId: "UTC",
      currency: "USD",
      restrictMaterialsOnDebt: false,
      debtGraceDays: 7,
      invoiceNumberTemplate: "A-{NNNN}",
    });

    await page.goto("/school/settings");

    const input = page.getByLabel("Шаблон номера");
    await expect(input).toHaveValue("A-{NNNN}");
    await expect(input).toBeDisabled();
  });
});
