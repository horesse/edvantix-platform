import { expect, test } from "@playwright/test";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installAdminShellMocks, ADMIN_PERMS, paged } from "../helpers/shell-mocks";
import { mockJsonResponse } from "../helpers/api-mocks";

// The platform dashboard ("/") is protected. On load it fires:
//   GET /api/v1/tenants/?PageNumber=1&PageSize=100   (list, drives KPIs + names)
//   GET /api/v1/billing/invoices?pageNumber=1&pageSize=50
//   GET /api/v1/billing/usage                         (all-tenant snapshots)
//   GET /api/v1/tenants/{id}/status                   (per-tenant fan-out for "по тарифам")

const SOON = new Date(Date.now() + 12 * 86_400_000).toISOString();
const LATER = new Date(Date.now() + 400 * 86_400_000).toISOString();

const TENANTS_PAGE = paged(
  [
    { id: "acme", name: "Acme Corp", adminEmail: "admin@acme.com", isActive: true, validUpto: LATER },
    { id: "globex", name: "Globex", adminEmail: "admin@globex.com", isActive: true, validUpto: SOON },
    { id: "initech", name: "Initech", adminEmail: "admin@initech.com", isActive: false, validUpto: LATER },
  ],
  { pageNumber: 1, pageSize: 100, totalCount: 3 },
);

const INVOICES_PAGE = paged(
  [
    { id: "inv-1", tenantId: "acme", invoiceNumber: "INV-0001", periodYear: 2026, periodMonth: 8, currency: "USD", subtotalAmount: 49, status: "Issued", createdAtUtc: "2026-08-01T00:00:00Z", lineItems: [] },
  ],
  { pageNumber: 1, pageSize: 50, totalCount: 1 },
);

// One tenant near a limit: Acme at 92% of ActiveStudents.
const USAGE = [
  { id: "u1", tenantId: "acme", periodYear: 2026, periodMonth: 8, resource: "ActiveStudents", usedUnits: 138, limitUnits: 150, overage: 0, capturedAtUtc: "2026-08-31T00:00:00Z" },
  { id: "u2", tenantId: "acme", periodYear: 2026, periodMonth: 8, resource: "ActiveTeachers", usedUnits: 4, limitUnits: 12, overage: 0, capturedAtUtc: "2026-08-31T00:00:00Z" },
  { id: "u3", tenantId: "globex", periodYear: 2026, periodMonth: 8, resource: "ActiveStudents", usedUnits: 3, limitUnits: 30, overage: 0, capturedAtUtc: "2026-08-31T00:00:00Z" },
];

const STATUS = {
  acme: { id: "acme", name: "Acme Corp", isActive: true, validUpto: LATER, adminEmail: "admin@acme.com", plan: "pro-annual", expiryState: "Active", graceEndsUtc: LATER },
  globex: { id: "globex", name: "Globex", isActive: true, validUpto: SOON, adminEmail: "admin@globex.com", plan: "free", expiryState: "Active", graceEndsUtc: SOON },
  initech: { id: "initech", name: "Initech", isActive: false, validUpto: LATER, adminEmail: "admin@initech.com", plan: "free", expiryState: "Active", graceEndsUtc: LATER },
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] });
  await installAdminShellMocks(page);

  await mockJsonResponse(page, "**/api/v1/tenants/?*", TENANTS_PAGE);
  await mockJsonResponse(page, "**/api/v1/billing/invoices**", INVOICES_PAGE);
  await mockJsonResponse(page, "**/api/v1/billing/usage**", USAGE);
  for (const [id, body] of Object.entries(STATUS)) {
    await mockJsonResponse(page, `**/api/v1/tenants/${id}/status`, body);
  }
});

test.describe("platform dashboard", () => {
  test("greets the operator by first name in the hero heading", async ({ page }) => {
    await page.goto("/");
    await expect(
      page.getByRole("heading", { name: /Обзор платформы,\s*Root/i }),
    ).toBeVisible({ timeout: 10_000 });
  });

  test("renders the four platform KPI tiles", async ({ page }) => {
    await page.goto("/");
    const main = page.getByRole("main");
    const kpiLabel = (text: string) => main.locator("div.meta", { hasText: text });

    await expect(kpiLabel("Школы")).toBeVisible({ timeout: 10_000 });
    await expect(kpiLabel("Активные")).toBeVisible();
    await expect(kpiLabel("Истекают")).toBeVisible();
    await expect(kpiLabel("У лимитов")).toBeVisible();

    // Истекают = 1 (Globex within 45d); У лимитов = 1 (Acme ActiveStudents ≥ 80%).
    await expect(main.getByText("в ближайшие 45 дн.")).toBeVisible();
    await expect(main.getByText("≥ 80% лимита тарифа")).toBeVisible();
  });

  test("shows schools-by-plan, expiring subscriptions and near-limit sections", async ({ page }) => {
    await page.goto("/");
    const main = page.getByRole("main");

    await expect(main.getByRole("heading", { name: "Школы по тарифам" })).toBeVisible({ timeout: 10_000 });
    // Plan keys resolved from the per-tenant status fan-out.
    await expect(main.getByText("pro-annual", { exact: true })).toBeVisible();
    await expect(main.getByText("free", { exact: true }).first()).toBeVisible();

    await expect(main.getByRole("heading", { name: "Истекающие подписки" })).toBeVisible();
    await expect(main.getByRole("link", { name: /Globex/ })).toBeVisible();

    await expect(main.getByRole("heading", { name: "Приближаются к лимитам" })).toBeVisible();
    // Acme near ActiveStudents limit: label "ученики" + "138 / 150 · 92%".
    await expect(main.getByText(/138 \/ 150 · 92%/)).toBeVisible();
  });

  test("renders the entry-point pivot cards with Russian labels", async ({ page }) => {
    await page.goto("/");
    const main = page.getByRole("main");
    await expect(main.getByText("Точки входа")).toBeVisible({ timeout: 10_000 });

    await expect(main.getByRole("link", { name: /Школы/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /Пользователи/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /Биллинг/ })).toBeVisible();
    await expect(main.getByRole("link", { name: /Счета/ })).toBeVisible();

    // Terminology: no visible "Tenant" copy on the dashboard.
    await expect(main.getByText(/\bTenant\b/)).toHaveCount(0);
  });
});
