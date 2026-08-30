import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const SID = "00000000-0000-0000-0000-0000000000a1";
const G1 = "a0000000-0000-0000-0000-000000000001";
const G2 = "a0000000-0000-0000-0000-000000000002";

const DETAIL = {
  id: SID,
  lastName: "Иванов",
  firstName: "Пётр",
  middleName: null,
  displayName: "Иванов Пётр",
  birthDate: "2010-05-01",
  phone: "",
  email: "petya@acme.com",
  userId: null,
  status: "Active",
  source: null,
  avatarFileId: null,
  managerUserId: "u-test-1",
  enrolledAtUtc: "2025-09-01T00:00:00Z",
  createdAtUtc: "2025-09-01T00:00:00Z",
  updatedAtUtc: null,
  guardianCount: 0,
  noteCount: 0,
};

function sg(id: string, code: string, name: string, status = "Active") {
  return {
    id,
    code,
    name,
    courseId: "c1",
    primaryTeacherId: "t1",
    format: "Offline",
    capacity: 8,
    activeEnrollmentCount: 1,
    startDate: "2026-02-01",
    endDate: null,
    status,
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
  };
}

function enr(id: string, groupId: string, status: string, over: Record<string, unknown> = {}) {
  return {
    id,
    studyGroupId: groupId,
    studentId: SID,
    enrolledOn: "2026-02-01",
    leftOn: null,
    status,
    leaveReason: null,
    tariffId: null,
    discountPercent: 0,
    ...over,
  };
}

const PERMS = [
  "Permissions.People.Students.View",
  "Permissions.StudyGroups.Enrollments.View",
];

test.describe("people/students/:id — Группы tab", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockJsonResponse(page, `**/api/v1/students/${SID}/guardians`, []);
    await mockJsonResponse(page, `**/api/v1/students/${SID}`, DETAIL);
    await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([
      sg(G1, "ENG-A1", "Английский A1"),
      sg(G2, "ENG-A0", "Английский A0", "Finished"),
    ]));
  });

  test("lists all groups including finished/left, linking to the group card", async ({
    page,
  }) => {
    await mockJsonResponse(page, `**/api/v1/students/${SID}/enrollments`, [
      enr("e1", G1, "Active"),
      enr("e2", G2, "Left", { leftOn: "2026-01-15", leaveReason: "Перевод" }),
    ]);
    await page.goto(`/students/${SID}`);
    await page.getByRole("button", { name: "Группы" }).click();

    await expect(page.getByText("Английский A1")).toBeVisible();
    await expect(page.getByText("Английский A0")).toBeVisible();
    await expect(page.getByText("Ушёл").first()).toBeVisible();
    await expect(page.getByText("Активен").first()).toBeVisible();
    await expect(
      page.getByRole("link", { name: /Английский A1/ }),
    ).toHaveAttribute("href", `/study-groups/${G1}`);
  });

  test("empty state when the student has no enrollments", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/students/${SID}/enrollments`, []);
    await page.goto(`/students/${SID}`);
    await page.getByRole("button", { name: "Группы" }).click();
    await expect(
      page.getByText("Ученик пока не состоял ни в одной группе."),
    ).toBeVisible();
  });

  test("Группы tab is hidden without Enrollments.View", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/identity/permissions", [
      "Permissions.People.Students.View",
    ]);
    await mockJsonResponse(page, `**/api/v1/students/${SID}/enrollments`, []);
    await page.goto(`/students/${SID}`);
    await expect(page.getByRole("button", { name: "Группы" })).toHaveCount(0);
  });
});
