import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

const PLANS = [
  { id: "p-free", key: "free", name: "Free", currency: "USD", monthlyBasePrice: 0, overageRates: {}, isActive: true, interval: "Monthly", annualPrice: null },
  { id: "p-pro", key: "pro", name: "Pro", currency: "USD", monthlyBasePrice: 29, overageRates: {}, isActive: true, interval: "Monthly", annualPrice: null },
  { id: "p-pro-yr", key: "pro-annual", name: "Pro (Annual)", currency: "USD", monthlyBasePrice: 29, overageRates: {}, isActive: true, interval: "Yearly", annualPrice: 290 },
];

function mockPlans(page: import("@playwright/test").Page) {
  return page.route("**/api/v1/billing/plans?*", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(PLANS),
    }),
  );
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
  // Match only the list query (…/tenants/?PageNumber=…) so it doesn't shadow
  // resource routes like …/tenants/acme-corp/status.
  await page.route("**/api/v1/tenants/?Page*", (route) =>
    route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(paged([])),
    }),
  );
});

test.describe("create tenant — plan selector", () => {
  test("shows the plan select, preselects the trial plan, and posts planKey", async ({ page }) => {
    await mockPlans(page);
    await page.route("**/api/v1/tenants/", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id: "acme-corp", status: "Queued" }),
      });
    });
    // Detail loads after the success navigation.
    await page.route("**/api/v1/tenants/*/status", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "acme-corp", name: "Acme Corp", adminEmail: "admin@acme.example",
          isActive: true, validUpto: "2027-01-01T00:00:00Z", issuer: "acme-corp.issuer",
          plan: "pro", expiryState: "Active", graceEndsUtc: "2027-01-08T00:00:00Z",
        }),
      }),
    );
    await page.route("**/api/v1/tenants/*/provisioning", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: JSON.stringify({ status: "Running", steps: [], correlationId: "x" }) }),
    );
    await page.route("**/api/v1/tenants/theme", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "{}" }),
    );
    await page.route("**/api/v1/identity/impersonation/grants**", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "[]" }),
    );

    await page.goto("/tenants");
    await page.getByRole("button", { name: "Новая школа", exact: true }).click();

    const dialog = page.getByRole("dialog");
    const planSelect = dialog.locator("#ct-plan");
    await expect(planSelect).toBeVisible({ timeout: 10_000 });
    // Trial plan ("free") is preselected once plans load.
    await expect(planSelect).toContainText("Free");

    // Operator switches to Pro: open the dropdown and pick the Pro item.
    // "Free" doesn't match, and "Pro" precedes "Pro (Annual)" in the list.
    await planSelect.click();
    await page.getByRole("menuitem", { name: "Pro" }).first().click();

    // Identifier (and JWT issuer) auto-derive from the display name — no need to
    // type the slug by hand.
    await dialog.getByLabel(/^Название/).fill("Acme Corp");
    await expect(dialog.getByLabel(/^Идентификатор/)).toHaveValue("acme-corp");
    await dialog.getByLabel(/^E-mail администратора/).fill("admin@acme.example");
    await dialog.getByLabel(/^Начальный пароль администратора/).fill("Sup3rSecret!");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/tenants/") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Создать школу", exact: true }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({ id: "acme-corp", planKey: "pro" });
  });
});

test.describe("tenant detail — renew", () => {
  test("shows plan + grace badge and renews via the dialog", async ({ page }) => {
    await mockPlans(page);
    await page.route("**/api/v1/tenants/acme-corp/status", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "acme-corp", name: "Acme Corp", adminEmail: "admin@acme.example",
          isActive: true, validUpto: "2026-05-01T00:00:00Z", issuer: "acme-corp.issuer",
          plan: "pro", expiryState: "InGrace", graceEndsUtc: "2026-05-08T00:00:00Z",
        }),
      }),
    );
    await page.route("**/api/v1/tenants/*/provisioning", (route) =>
      route.fulfill({ status: 404, headers: { "Content-Type": "application/json" }, body: "{}" }),
    );
    // Theme must carry the palette keys; the branding card's ThemePreview reads
    // palette.background, so an empty {} crashes the detail page (undefined.background).
    await page.route("**/api/v1/tenants/theme", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          lightPalette: {}, darkPalette: {}, brandAssets: {},
          typography: { fontFamily: "Inter", headingFontFamily: "Inter", fontSizeBase: 14, lineHeightBase: 1.5 },
          layout: { borderRadius: "4px", defaultElevation: 1 },
          isDefault: true,
        }),
      }),
    );
    await page.route("**/api/v1/identity/impersonation/grants**", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "[]" }),
    );

    await page.goto("/tenants/acme-corp");

    // Page + tenant loaded (the renew action is inside the tenant-loaded block).
    const renewButton = page.getByRole("button", { name: /Продлить \/ сменить тариф/ });
    await expect(renewButton).toBeVisible({ timeout: 10_000 });

    // Plan + grace badges render in the hero.
    await expect(page.getByText("Льготный период").first()).toBeVisible();
    await expect(page.getByText("pro").first()).toBeVisible();

    // Open the renew dialog.
    await renewButton.click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Продление подписки" })).toBeVisible();

    // Renew the current plan.
    const renewReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/tenants/acme-corp/renew") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.route("**/api/v1/tenants/acme-corp/renew", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ tenantId: "acme-corp", validUpto: "2026-06-01T00:00:00Z", planKey: "pro", planChanged: false }),
      }),
    );
    await dialog.getByRole("button", { name: /^Продлить$/ }).click();
    const req = await renewReq;
    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({ tenantId: "acme-corp", planKey: "pro" });
  });
});

test.describe("tenant detail — adjust validity", () => {
  test("posts the expected body to /adjust-validity", async ({ page }) => {
    await mockPlans(page);
    await page.route("**/api/v1/tenants/acme-corp/status", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          id: "acme-corp", name: "Acme Corp", adminEmail: "admin@acme.example",
          isActive: true, validUpto: "2026-05-01T00:00:00Z", issuer: "acme-corp.issuer",
          plan: "pro", expiryState: "Active", graceEndsUtc: "2026-05-08T00:00:00Z",
        }),
      }),
    );
    await page.route("**/api/v1/tenants/*/provisioning", (route) =>
      route.fulfill({ status: 404, headers: { "Content-Type": "application/json" }, body: "{}" }),
    );
    await page.route("**/api/v1/tenants/theme", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          lightPalette: {}, darkPalette: {}, brandAssets: {},
          typography: { fontFamily: "Inter", headingFontFamily: "Inter", fontSizeBase: 14, lineHeightBase: 1.5 },
          layout: { borderRadius: "4px", defaultElevation: 1 },
          isDefault: true,
        }),
      }),
    );
    await page.route("**/api/v1/identity/impersonation/grants**", (route) =>
      route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: "[]" }),
    );

    await page.goto("/tenants/acme-corp");

    // The adjust button is gated behind UpgradeSubscription (in ADMIN_PERMS).
    const adjustButton = page.getByRole("main").getByRole("button", { name: /Скорректировать срок/ });
    await expect(adjustButton).toBeVisible({ timeout: 10_000 });
    await adjustButton.click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Корректировка срока" })).toBeVisible();

    await dialog.getByLabel(/^Действует до/).fill("2026-09-15");

    const adjustReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/tenants/acme-corp/adjust-validity") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.route("**/api/v1/tenants/acme-corp/adjust-validity", (route) =>
      route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ tenantId: "acme-corp", validUpto: "2026-09-15T00:00:00Z" }),
      }),
    );
    await dialog.getByRole("button", { name: "Скорректировать срок", exact: true }).click();
    const req = await adjustReq;

    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.tenantId).toBe("acme-corp");
    // validUpto is sent as an ISO 8601 string for the picked date.
    expect(body.validUpto).toMatch(/^2026-09-15T/);
    expect(Number.isNaN(new Date(body.validUpto).getTime())).toBe(false);
  });
});

test.describe("plan dialog — billing interval", () => {
  test("yearly reveals the annual price field and posts interval + annualPrice", async ({ page }) => {
    await mockPlans(page); // plans list query
    await page.route("**/api/v1/billing/plans", async (route) => {
      if (route.request().method() !== "POST") {
        await route.fallback();
        return;
      }
      await route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: JSON.stringify("p-new") });
    });

    await page.goto("/billing/plans");
    await page.getByRole("button", { name: "Новый тариф", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Новый тариф", exact: true })).toBeVisible({ timeout: 10_000 });
    // Annual price is hidden for monthly plans.
    await expect(dialog.getByLabel(/^Цена за год/)).toBeHidden();

    await dialog.getByRole("button", { name: "Период списания" }).click();
    await page.getByRole("menuitem", { name: "Ежегодно" }).click();
    await expect(dialog.getByLabel(/^Цена за год/)).toBeVisible();

    await dialog.getByLabel(/^Ключ/).fill("team-annual");
    await dialog.getByLabel(/^Отображаемое название/).fill("Team Annual");
    await dialog.getByLabel(/^Валюта/).fill("USD");
    await dialog.getByLabel(/^Базовая цена в месяц/).fill("50");
    await dialog.getByLabel(/^Цена за год/).fill("500");

    const reqPromise = page.waitForRequest(
      (r) => r.url().endsWith("/api/v1/billing/plans") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await dialog.getByRole("button", { name: "Создать тариф", exact: true }).click();
    const req = await reqPromise;

    expect(JSON.parse(req.postData() ?? "{}")).toMatchObject({
      key: "team-annual", interval: "Yearly", annualPrice: 500, monthlyBasePrice: 50,
    });
  });

  test("rejects a negative monthly price client-side with no network call", async ({ page }) => {
    await mockPlans(page); // plans list query

    // Fail the test if a create POST is ever attempted.
    let posted = false;
    await page.route("**/api/v1/billing/plans", async (route) => {
      if (route.request().method() === "POST") {
        posted = true;
        await route.fulfill({ status: 200, headers: { "Content-Type": "application/json" }, body: JSON.stringify("p-new") });
        return;
      }
      await route.fallback();
    });

    await page.goto("/billing/plans");
    await page.getByRole("button", { name: "Новый тариф", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Новый тариф", exact: true })).toBeVisible({ timeout: 10_000 });

    await dialog.getByLabel(/^Ключ/).fill("cheap");
    await dialog.getByLabel(/^Отображаемое название/).fill("Cheap");
    await dialog.getByLabel(/^Валюта/).fill("USD");
    await dialog.getByLabel(/^Базовая цена в месяц/).fill("-5");

    // The client-side zod refinement surfaces the error and disables submit.
    await expect(dialog.getByText("Должно быть неотрицательным числом.")).toBeVisible();

    const submit = dialog.getByRole("button", { name: "Создать тариф", exact: true });
    await expect(submit).toBeDisabled();
    // Force a click even while disabled to prove the guard holds — no request fires.
    await submit.click({ force: true });
    await page.waitForTimeout(300);
    expect(posted).toBe(false);
  });

  test("editing a plan opens the dialog prefilled", async ({ page }) => {
    await mockPlans(page);
    await page.goto("/billing/plans");

    // Edit the seeded Pro plan via its row action.
    await page.getByRole("button", { name: "Изменить тариф «Pro»", exact: true }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Изменить тариф", exact: true })).toBeVisible({ timeout: 10_000 });
    await expect(dialog.getByLabel(/^Отображаемое название/)).toHaveValue("Pro");
    // Key is immutable when editing.
    await expect(dialog.getByLabel(/^Ключ/)).toBeDisabled();
  });
});
