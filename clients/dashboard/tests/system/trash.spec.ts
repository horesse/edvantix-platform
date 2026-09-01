import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// Trash is tabbed. Default tab = Courses → GET /api/v1/courses/trash.
// The trash row VM only reads { id, title, slug } for courses (deleted
// metadata isn't on CourseDto), so a lean course shape is enough.
function trashedCourse(over: Record<string, unknown> = {}) {
  return {
    id: "co-1",
    subjectId: "s-1",
    title: "Английский A1",
    slug: "english-a1",
    level: "A1",
    durationHours: 40,
    status: "Archived",
    createdAtUtc: "2026-05-01T00:00:00.000Z",
    ...over,
  };
}

function trashedTicket(over: Record<string, unknown> = {}) {
  return {
    id: "t-9",
    number: "TS-0009",
    title: "Ошибка в счёте",
    status: "Open",
    priority: "Medium",
    category: "Payment",
    reporterUserId: "11111111-2222-3333-4444-555555555555",
    commentCount: 0,
    createdAtUtc: "2026-05-01T00:00:00.000Z",
    deletedOnUtc: "2026-05-19T09:00:00.000Z",
    deletedBy: "11111111-2222-3333-4444-555555555555",
    ...over,
  };
}

// Trash tabs are permission-gated (mirrors src/lib/trash-permissions.ts). The
// dashboard reads the user's permission set from GET /identity/permissions, so
// tests grant tabs by re-mocking that endpoint AFTER installShellMocks (which
// defaults it to []); Playwright matches the most-recently-registered route.
const TRASH_PERMS = {
  courses: "Permissions.Curriculum.Courses.Restore",
  tickets: "Permissions.Tickets.Restore",
  files: "Permissions.Files.ViewTrash",
} as const;

const ALL_TRASH_PERMS = Object.values(TRASH_PERMS);

async function grantPermissions(page: Page, perms: readonly string[]): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  // Default: the user can reach every trash tab. Gating-specific tests override.
  await grantPermissions(page, ALL_TRASH_PERMS);
});

test.describe("system/trash", () => {
  test("renders the 'Recycle bin' heading + a trashed course row (default tab)", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/courses/trash**",
      paged([trashedCourse({ title: "Английский A1" })], { totalCount: 1 }),
    );

    await page.goto("/system/trash");

    await expect(
      page.getByRole("heading", { name: "Recycle bin", level: 1 }),
    ).toBeVisible();

    // Row title renders in the hidden mobile card AND the desktop row →
    // assert on the last (visible desktop) occurrence.
    await expect(page.getByText("Английский A1").last()).toBeVisible();
    await expect(page.getByText(/1 courses in trash/i)).toBeVisible();
    await expect(page.getByRole("button", { name: /restore/i }).last()).toBeVisible();
  });

  test("renders the empty state for the courses tab", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/courses/trash**", paged([], { totalCount: 0 }));

    await page.goto("/system/trash");

    await expect(
      page.getByRole("heading", { name: /the courses trash is empty/i }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: /back to courses/i })).toBeVisible();
  });

  test("switching to the Tickets tab loads the tickets trash endpoint", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/courses/trash**",
      paged([trashedCourse()], { totalCount: 1 }),
    );
    await mockJsonResponse(
      page,
      "**/api/v1/tickets/trash**",
      paged([trashedTicket({ title: "Ошибка в счёте" })], { totalCount: 1 }),
    );

    await page.goto("/system/trash");
    await expect(
      page.getByRole("heading", { name: "Recycle bin", level: 1 }),
    ).toBeVisible();

    const reqPromise = page.waitForRequest(
      (r) => r.url().includes("/api/v1/tickets/trash"),
      { timeout: 5_000 },
    );
    await page
      .getByRole("navigation", { name: /trash sections/i })
      .getByRole("button", { name: "Tickets" })
      .click();
    await reqPromise;

    await expect(page.getByText("Ошибка в счёте").last()).toBeVisible();
    await expect(page.getByText(/1 tickets in trash/i)).toBeVisible();
  });

  test("hides tabs the user lacks permission for, defaulting to the first visible one", async ({
    page,
  }) => {
    // Only Tickets + Files are reachable — Courses (the hard-coded default tab)
    // is gated away, so the page must fall back to the first visible tab.
    await grantPermissions(page, [TRASH_PERMS.tickets, TRASH_PERMS.files]);
    await mockJsonResponse(
      page,
      "**/api/v1/tickets/trash**",
      paged([trashedTicket()], { totalCount: 1 }),
    );

    await page.goto("/system/trash");

    const tabs = page.getByRole("navigation", { name: /trash sections/i });
    await expect(tabs.getByRole("button", { name: "Tickets" })).toBeVisible();
    await expect(tabs.getByRole("button", { name: "Files" })).toBeVisible();
    // Gated tabs are absent entirely — not just disabled.
    await expect(tabs.getByRole("button", { name: "Courses" })).toHaveCount(0);
    // Catalog tabs are gone for good.
    await expect(tabs.getByRole("button", { name: "Products" })).toHaveCount(0);
    await expect(tabs.getByRole("button", { name: "Brands" })).toHaveCount(0);

    await expect(page.getByText(/1 tickets in trash/i)).toBeVisible();
  });

  test("shows a no-access empty state when the user has no trash permissions", async ({
    page,
  }) => {
    await grantPermissions(page, []);

    await page.goto("/system/trash");

    await expect(
      page.getByRole("heading", { name: /no recycle bins available/i }),
    ).toBeVisible();
    await expect(
      page.getByRole("navigation", { name: /trash sections/i }),
    ).toHaveCount(0);
  });
});
