import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// Stage 7 rebuilt src/components/layout/nav-data.ts to the target menu from
// docs/03 Frontend/Dashboard (школа).md. These specs pin the section layout,
// the permission gating of each item, the «Группы доступа» rename, and the
// removal of the Catalog section.

async function grant(page: Page, perms: readonly string[]): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);
}

/** The desktop sidebar landmark (an <aside aria-label="Primary navigation">). */
function sidebar(page: Page) {
  return page.locator('aside[aria-label="Primary navigation"]');
}

const MANAGER_PERMS = [
  "Permissions.People.Students.View",
  "Permissions.People.Teachers.View",
  "Permissions.Curriculum.Courses.View",
  "Permissions.StudyGroups.StudyGroups.View",
  "Permissions.Scheduling.Sessions.View",
  "Permissions.Payments.StudentInvoices.View",
  "Permissions.Tickets.View",
  "Permissions.Billing.View",
  "Permissions.Users.Update",
  "Permissions.Roles.Update",
  "Permissions.Groups.Update",
  "Permissions.AuditTrails.View",
];

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  // Every list endpoint a nav target might hit → empty, so pages don't hang.
  await mockJsonResponse(page, "**/api/v1/identity/groups", []);
  await mockJsonResponse(page, "**/api/v1/students**", paged([]));
});

test.describe("dashboard navigation — section layout", () => {
  test("renders the Stage 7 sections and drops the old ones", async ({ page }) => {
    await grant(page, MANAGER_PERMS);
    await page.goto("/");

    const nav = sidebar(page);
    // New sections.
    for (const caption of [
      "Люди",
      "Учебный процесс",
      "Оплаты",
      "Хелпдеск",
      "Подписка",
      "Идентификация",
      "Система",
    ]) {
      await expect(nav.getByRole("button", { name: caption })).toBeVisible();
    }
    // Old sections are gone.
    for (const caption of [
      "Catalog",
      "Программа",
      "Учебные группы",
      "Расписание",
      "Operations",
      "Helpdesk",
      "Identity",
      "System",
    ]) {
      await expect(nav.getByRole("button", { name: caption })).toHaveCount(0);
    }
    // No Catalog item links survive anywhere in the nav tree.
    await expect(nav.getByRole("link", { name: /Products|Brands|Categories/ })).toHaveCount(0);
  });

  test("top-level items use the Russian labels", async ({ page }) => {
    await grant(page, ["Permissions.Chat.Channels.View", "Permissions.Files.Upload"]);
    await page.goto("/");
    const nav = sidebar(page);
    await expect(nav.getByRole("link", { name: "Обзор" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Чат" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Файлы" })).toBeVisible();
    await expect(nav.getByRole("link", { name: "Настройки" })).toBeVisible();
  });
});

test.describe("dashboard navigation — permission gating", () => {
  test("a section is hidden when the user lacks its list permission", async ({ page }) => {
    // Only students → «Люди» shows, «Оплаты» / «Хелпдеск» / «Идентификация» don't.
    await grant(page, ["Permissions.People.Students.View"]);
    await page.goto("/");

    const nav = sidebar(page);
    await expect(nav.getByRole("button", { name: "Люди" })).toBeVisible();
    await expect(nav.getByRole("button", { name: "Оплаты" })).toHaveCount(0);
    await expect(nav.getByRole("button", { name: "Хелпдеск" })).toHaveCount(0);
    await expect(nav.getByRole("button", { name: "Идентификация" })).toHaveCount(0);
  });

  test("«Учебный процесс» needs one of its five list permissions", async ({ page }) => {
    await grant(page, ["Permissions.Scheduling.Attendance.View"]);
    await page.goto("/");
    await expect(sidebar(page).getByRole("button", { name: "Учебный процесс" })).toBeVisible();
  });
});

test.describe("dashboard navigation — «Группы доступа» rename", () => {
  test("the identity group entry is labelled «Группы доступа», route unchanged", async ({
    page,
  }) => {
    await grant(page, ["Permissions.Groups.Update"]);
    // Land on the groups route so the «Идентификация» accordion auto-opens.
    await page.goto("/identity/groups");

    const nav = sidebar(page);
    const link = nav.getByRole("link", { name: "Группы доступа" });
    await expect(link).toBeVisible();
    await expect(link).toHaveAttribute("href", "/identity/groups");
    // The old English label is gone.
    await expect(nav.getByRole("link", { name: "Groups", exact: true })).toHaveCount(0);
  });
});
