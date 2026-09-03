import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const ID = "00000000-0000-0000-0000-0000000000a1";
const AUDIT_ID = "aud-1";

const DETAIL = {
  id: ID,
  lastName: "Иванов",
  firstName: "Пётр",
  middleName: "Сергеевич",
  displayName: "Иванов Пётр Сергеевич",
  birthDate: "2010-05-01",
  phone: "+7 900 111-22-33",
  email: "petya@acme.com",
  userId: null,
  status: "Active",
  source: "Сайт",
  avatarFileId: null,
  managerUserId: "u-test-1",
  enrolledAtUtc: "2025-09-01T00:00:00Z",
  createdAtUtc: "2025-09-01T00:00:00Z",
  updatedAtUtc: null,
  guardianCount: 0,
  noteCount: 0,
};

const AUDIT_ROW = {
  id: AUDIT_ID,
  occurredAtUtc: "2026-02-01T09:30:00Z",
  eventType: "EntityChange",
  severity: "Information",
  userId: "u-test-1",
  userName: "Иван Менеджер",
  tags: 0,
};

const AUDIT_DETAIL = {
  ...AUDIT_ROW,
  receivedAtUtc: "2026-02-01T09:30:01Z",
  payload: {
    dbContext: "PeopleDbContext",
    entityName: "Student",
    key: `Id:${ID}`,
    operation: "Update",
    changes: [{ name: "Status", dataType: "string", oldValue: "Active", newValue: "Archived" }],
  },
};

const LABELS = {
  entities: { Student: "Ученик" },
  fields: { Status: "Статус" },
};

const WITH_AUDIT = [
  "Permissions.People.Students.View",
  "Permissions.AuditTrails.View",
];
const WITHOUT_AUDIT = ["Permissions.People.Students.View"];

async function seed(page: import("@playwright/test").Page, perms: string[]) {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);
  await mockJsonResponse(page, `**/api/v1/students/${ID}`, DETAIL);
  await mockJsonResponse(page, `**/api/v1/students/${ID}/guardians`, []);
  await mockJsonResponse(page, `**/api/v1/students/${ID}/notes`, []);
  // Audit detail (single-segment) first, then the more-specific overrides.
  await mockJsonResponse(page, "**/api/v1/audits/*", AUDIT_DETAIL);
  await mockJsonResponse(page, "**/api/v1/audits/entity-labels", LABELS);
  await mockJsonResponse(
    page,
    "**/api/v1/audits/by-entity/Student/**",
    paged([AUDIT_ROW]),
  );
}

test.describe("people/students/:id — history tab", () => {
  test("renders audit rows and expands to the relabelled changed fields", async ({ page }) => {
    await seed(page, WITH_AUDIT);
    await page.goto(`/students/${ID}`);

    await page.getByRole("button", { name: "История" }).click();
    await expect(page.getByText("Иван Менеджер")).toBeVisible();

    await page.getByRole("button", { expanded: false }).filter({ hasText: "Иван Менеджер" }).click();
    await expect(page.getByText("Статус")).toBeVisible();
    await expect(page.getByText("Active → Archived")).toBeVisible();
  });

  test("history tab is hidden without Permissions.AuditTrails.View", async ({ page }) => {
    await seed(page, WITHOUT_AUDIT);
    await page.goto(`/students/${ID}`);

    await expect(
      page.getByRole("heading", { name: "Иванов Пётр Сергеевич", level: 1 }),
    ).toBeVisible();
    await expect(page.getByRole("button", { name: "История" })).toBeHidden();
  });
});
