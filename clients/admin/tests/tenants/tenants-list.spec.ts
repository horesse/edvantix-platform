import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";

const TENANT_ACME = {
  id: "acme",
  name: "Acme Corp",
  adminEmail: "admin@acme.com",
  isActive: true,
  validUpto: "2027-01-01T00:00:00Z",
  issuer: "fsh.demo.acme",
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);
});

test.describe("tenants registry list", () => {
  test("renders the Школы heading and a school row from the mock", async ({ page }) => {
    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([TENANT_ACME])),
      });
    });

    await page.goto("/tenants");

    await expect(
      page.getByRole("heading", { name: "Школы", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // The tenant row from our mock. The name renders in both a (hidden) mobile
    // card and the desktop row, so scope to the desktop row button — its
    // accessible name carries the tenant name + id. The admin email also appears
    // in both variants (the mobile one is display:none on desktop), so scope it
    // to the desktop row button to assert the visible occurrence.
    const desktopRow = page.getByRole("button", { name: /Acme Corp/ });
    await expect(desktopRow).toBeVisible();
    await expect(desktopRow.getByText("admin@acme.com", { exact: true })).toBeVisible();
  });

  test("shows the empty state when no tenants are registered", async ({ page }) => {
    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([])),
      });
    });

    await page.goto("/tenants");

    await expect(page.getByText("Школ пока нет.", { exact: true })).toBeVisible({
      timeout: 10_000,
    });
    await expect(
      page.getByText("Заведите первую школу, чтобы начать.", { exact: true }),
    ).toBeVisible();
  });

  test("the New tenant button opens the create dialog", async ({ page }) => {
    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([TENANT_ACME])),
      });
    });

    await page.goto("/tenants");
    await expect(
      page.getByRole("heading", { name: "Школы", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    // Creation is now an in-page dialog, not a /tenants/new route.
    await page.getByRole("button", { name: "Новая школа", exact: true }).click();

    await expect(page).toHaveURL(/\/tenants$/);
    const dialog = page.getByRole("dialog");
    await expect(
      dialog.getByRole("heading", { name: "Новая школа", exact: true }),
    ).toBeVisible();
  });

  test("hides the Новая школа button for a Tenants.View-only user", async ({ page }) => {
    // Keep Tenants.View (route guard) but drop Tenants.Create.
    const viewOnly = ADMIN_PERMS.filter((p) => p !== "Permissions.Tenants.Create");
    await seedAuthedSession(page, { ...TEST_USER, permissions: viewOnly });
    await installAdminShellMocks(page, viewOnly);

    await page.route("**/api/v1/tenants/?*", async (route) => {
      await route.fulfill({
        status: 200,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(paged([TENANT_ACME])),
      });
    });

    await page.goto("/tenants");
    await expect(
      page.getByRole("heading", { name: "Школы", exact: true }),
    ).toBeVisible({ timeout: 10_000 });

    await expect(page.getByRole("button", { name: "Новая школа", exact: true })).toHaveCount(0);
    // No visible "Tenant" copy anywhere on the screen.
    await expect(page.getByText(/\bTenant\b/)).toHaveCount(0);
  });
});
