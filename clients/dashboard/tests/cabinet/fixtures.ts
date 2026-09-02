import type { Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import type { SeededUser } from "../helpers/auth-seed";

/** Кабинетный пользователь (ученик Alice) — стабильный sub для ассертов. */
export const CABINET_USER: SeededUser = {
  sub: "u-cabinet-1",
  email: "student@acme.com",
  firstName: "Alice",
  lastName: "Nguyen",
  tenant: "acme",
  permissions: [],
};

export const PERMS = {
  sessionsViewOwn: "Permissions.Scheduling.Sessions.ViewOwn",
  groupsViewOwn: "Permissions.StudyGroups.StudyGroups.ViewOwn",
  invoicesViewOwn: "Permissions.Payments.StudentInvoices.ViewOwn",
  studentsView: "Permissions.People.Students.View",
} as const;

export type Scope = {
  studentId?: string | null;
  teacherId?: string | null;
  guardianId?: string | null;
  wardStudentIds?: string[];
};

/** Переопределяет GET /people/me/scope (installShellMocks ставит пустой). */
export async function mockScope(page: Page, scope: Scope): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/people/me/scope", {
    studentId: scope.studentId ?? null,
    teacherId: scope.teacherId ?? null,
    guardianId: scope.guardianId ?? null,
    wardStudentIds: scope.wardStudentIds ?? [],
  });
}

export async function mockTenantSettings(page: Page): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/tenants/settings", {
    timeZoneId: "Europe/Moscow",
    currency: "RUB",
    restrictMaterialsOnDebt: false,
    debtGraceDays: 7,
  });
}

/** EDX-015 — GET /student-invoices/my/materials-access. Register AFTER any broad
 *  `**\/student-invoices/my**` mock so this specific path wins. */
export async function mockMaterialsAccess(
  page: Page,
  status: { restricted: boolean; overdueSince?: string | null; graceDays?: number },
): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/student-invoices/my/materials-access", {
    restricted: status.restricted,
    overdueSince: status.overdueSince ?? (status.restricted ? "2026-08-01" : null),
    graceDays: status.graceDays ?? 7,
  });
}

export function session(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: "sess-1",
    studyGroupId: "grp-1",
    lessonId: null,
    teacherId: "t-1",
    roomId: null,
    startUtc: "2026-09-02T09:00:00Z",
    endUtc: "2026-09-02T10:30:00Z",
    status: "Planned",
    topic: "Present Simple",
    meetingUrl: null,
    scheduleTemplateId: null,
    rescheduledFromId: null,
    ...overrides,
  };
}

export function invoice(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    id: "inv-1",
    number: "SI-2026-0042",
    studentId: "stu-1",
    payerGuardianId: null,
    studyGroupId: "grp-1",
    periodFrom: "2026-09-01",
    periodTo: "2026-09-30",
    total: 6000,
    paidAmount: 0,
    currency: "RUB",
    status: "Issued",
    issuedOn: "2026-09-01",
    dueDate: "2026-09-10",
    isOverdue: false,
    comment: null,
    createdAtUtc: "2026-09-01T00:00:00Z",
    updatedAtUtc: null,
    ...overrides,
  };
}
