import { expect, test } from "@playwright/test";
import { mockJsonResponse, mockProblemDetails } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";

const TENANT_ID = "acme";

// RouteGuard on /tenants/:id requires Tenants.View. The branding card itself
// makes ViewTheme / UpdateTheme calls; the server enforces permissions on
// those calls, but in tests we mock the API so the in-app permissions list
// only needs to satisfy the page-level RouteGuard. We grant the full
// multitenancy permission set for simplicity.
const ROOT_PERMS = [
  "Permissions.Tenants.View",
  "Permissions.Tenants.ViewTheme",
  "Permissions.Tenants.UpdateTheme",
];

const TENANT = {
  id: TENANT_ID,
  name: "Acme Corp",
  adminEmail: "admin@acme.com",
  isActive: true,
  validUpto: "2027-01-01T00:00:00Z",
  issuer: "fsh.demo.acme",
};

const PROVISIONING = {
  status: "Completed",
  currentStep: "CacheWarm",
  correlationId: "abc-123",
  steps: [],
  startedUtc: "2026-05-10T10:00:00Z",
  completedUtc: "2026-05-10T10:00:08Z",
  error: null,
};

const THEME_DEFAULT = {
  lightPalette: {
    primary: "#2563EB",
    secondary: "#0F172A",
    tertiary: "#6366F1",
    background: "#F8FAFC",
    surface: "#FFFFFF",
    error: "#DC2626",
    warning: "#F59E0B",
    success: "#16A34A",
    info: "#0284C7",
  },
  darkPalette: {
    primary: "#38BDF8",
    secondary: "#94A3B8",
    tertiary: "#818CF8",
    background: "#0B1220",
    surface: "#111827",
    error: "#F87171",
    warning: "#FBBF24",
    success: "#22C55E",
    info: "#38BDF8",
  },
  brandAssets: {
    logoUrl: null,
    logoDarkUrl: null,
    faviconUrl: null,
    deleteLogo: false,
    deleteLogoDark: false,
    deleteFavicon: false,
  },
  typography: {
    fontFamily: "Inter, sans-serif",
    headingFontFamily: "Inter, sans-serif",
    fontSizeBase: 14,
    lineHeightBase: 1.5,
  },
  layout: { borderRadius: "4px", defaultElevation: 1 },
  isDefault: true,
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: ROOT_PERMS });
  await mockJsonResponse(page, "**/api/v1/identity/profile", {
    id: TEST_USER.sub,
    email: TEST_USER.email,
    isActive: true,
    emailConfirmed: true,
  });
  await mockJsonResponse(page, "**/api/v1/identity/permissions", []);
  await mockJsonResponse(page, `**/api/v1/tenants/${TENANT_ID}/status`, TENANT);
  await mockJsonResponse(
    page,
    `**/api/v1/tenants/${TENANT_ID}/provisioning`,
    PROVISIONING,
  );
  // Active grants list — not under test here, return empty.
  await mockJsonResponse(page, "**/api/v1/identity/impersonation/grants**", []);
});

test.describe("tenant branding card", () => {
  test("loads + renders the editor with default-palette badge", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/theme", THEME_DEFAULT);

    await page.goto(`/tenants/${TENANT_ID}`);

    // Wait for the branding card heading.
    const branding = page.locator("section, div").filter({ hasText: "Брендирование" }).first();
    await expect(branding).toBeVisible({ timeout: 10_000 });

    // Default badge (server returned isDefault: true).
    await expect(page.getByText("по умолчанию").first()).toBeVisible();

    // Both palette sections render.
    await expect(page.getByText(/светлая палитра/i)).toBeVisible();
    await expect(page.getByText(/тёмная палитра/i)).toBeVisible();

    // Brand asset URL fields render.
    await expect(page.getByLabel("URL логотипа", { exact: true })).toBeVisible();
    await expect(page.getByLabel("URL логотипа (тёмная тема)")).toBeVisible();
    await expect(page.getByLabel("URL favicon")).toBeVisible();
  });

  test("Save button is disabled until the operator edits something", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/theme", THEME_DEFAULT);

    await page.goto(`/tenants/${TENANT_ID}`);
    const save = page.getByRole("button", { name: /сохранить брендирование/i });
    await expect(save).toBeVisible({ timeout: 10_000 });
    await expect(save).toBeDisabled();

    // Edit the logo URL via the visible textbox.
    await page.getByLabel("URL логотипа", { exact: true }).fill("https://cdn.example.com/acme.svg");
    await expect(save).toBeEnabled();
    await expect(page.getByText("не сохранено").first()).toBeVisible();
  });

  test("Save PUTs the edited theme with the tenant header", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/theme", THEME_DEFAULT);

    await page.goto(`/tenants/${TENANT_ID}`);
    await expect(page.getByLabel("URL логотипа", { exact: true })).toBeVisible({ timeout: 10_000 });

    await page.getByLabel("URL логотипа", { exact: true }).fill("https://cdn.example.com/acme.svg");

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().endsWith("/api/v1/tenants/theme") && r.method() === "PUT",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /сохранить брендирование/i }).click();
    const req = await reqPromise;

    expect(req.headers().tenant).toBe(TENANT_ID);
    const body = JSON.parse(req.postData() ?? "{}");
    expect(body.brandAssets.logoUrl).toBe("https://cdn.example.com/acme.svg");
    expect(body.lightPalette).toMatchObject({ primary: "#2563EB" });
  });

  test("Reset POSTs to /theme/reset and shows a confirmation toast", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/tenants/theme", THEME_DEFAULT);
    await mockJsonResponse(page, "**/api/v1/tenants/theme/reset", '""');

    await page.goto(`/tenants/${TENANT_ID}`);
    await expect(page.getByRole("button", { name: /сбросить.*умолчани/i })).toBeVisible({
      timeout: 10_000,
    });

    const reqPromise = page.waitForRequest(
      (r) =>
        r.url().endsWith("/api/v1/tenants/theme/reset") && r.method() === "POST",
      { timeout: 5_000 },
    );
    await page.getByRole("button", { name: /сбросить.*умолчани/i }).click();
    const req = await reqPromise;
    expect(req.headers().tenant).toBe(TENANT_ID);

    await expect(page.getByText(/брендирование сброшено к умолчаниям/i)).toBeVisible();
  });

  test("surfaces a server error in the error band, not as a toast", async ({ page }) => {
    await mockProblemDetails(page, "**/api/v1/tenants/theme", 403, {
      title: "Forbidden",
      detail: "Tenant theme is read-only for this caller.",
    });

    await page.goto(`/tenants/${TENANT_ID}`);
    await expect(page.getByText(/read-only for this caller/i)).toBeVisible({
      timeout: 10_000,
    });
    // No save button when the editor never loaded.
    await expect(page.getByRole("button", { name: /сохранить брендирование/i })).not.toBeVisible();
  });
});
