import type { Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { paged } from "../helpers/shell-mocks";

// Stable ids reused across the Payments specs.
export const STUDENT_ID = "11111111-0000-0000-0000-000000000001";
export const STUDENT_2 = "11111111-0000-0000-0000-000000000002";
export const GROUP_ID = "22222222-0000-0000-0000-000000000001";
export const TARIFF_ID = "33333333-0000-0000-0000-000000000001";
export const PKG_TARIFF_ID = "33333333-0000-0000-0000-000000000002";
export const INVOICE_ID = "44444444-0000-0000-0000-000000000001";
export const DRAFT_ID = "44444444-0000-0000-0000-0000000000d1";
export const PAYMENT_ID = "55555555-0000-0000-0000-000000000001";

export const PERMS = {
  tariffsView: "Permissions.Payments.Tariffs.View",
  tariffsManage: "Permissions.Payments.Tariffs.Manage",
  invoicesView: "Permissions.Payments.StudentInvoices.View",
  invoicesViewOwn: "Permissions.Payments.StudentInvoices.ViewOwn",
  invoicesCreate: "Permissions.Payments.StudentInvoices.Create",
  invoicesIssue: "Permissions.Payments.StudentInvoices.Issue",
  invoicesCancel: "Permissions.Payments.StudentInvoices.Cancel",
  invoicesExport: "Permissions.Payments.StudentInvoices.Export",
  paymentsView: "Permissions.Payments.StudentPayments.View",
  paymentsConfirm: "Permissions.Payments.StudentPayments.Confirm",
  paymentsRevoke: "Permissions.Payments.StudentPayments.Revoke",
} as const;

export function tariff(over: Record<string, unknown> = {}) {
  return {
    id: TARIFF_ID,
    name: "Занятие A1",
    courseId: null,
    kind: "PerLesson",
    amount: 900,
    currency: "RUB",
    lessonsCount: 0,
    validDays: 0,
    chargeOnExcusedAbsence: false,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
    ...over,
  };
}

export function packageTariff(over: Record<string, unknown> = {}) {
  return tariff({
    id: PKG_TARIFF_ID,
    name: "Пакет 8 занятий",
    kind: "PerPackage",
    amount: 6400,
    lessonsCount: 8,
    validDays: 60,
    ...over,
  });
}

export function line(over: Record<string, unknown> = {}) {
  return {
    id: "l1",
    description: "Занятия за сентябрь",
    tariffId: TARIFF_ID,
    quantity: 8,
    unitPrice: 900,
    amount: 7200,
    ...over,
  };
}

export function payment(over: Record<string, unknown> = {}) {
  return {
    id: PAYMENT_ID,
    invoiceId: INVOICE_ID,
    amount: 5000,
    paidOn: "2026-09-10",
    method: "Cash",
    reference: "CHK-1",
    proofFileId: null,
    confirmedByUserId: "u-test-1",
    confirmedAtUtc: "2026-09-10T12:00:00Z",
    reversesId: null,
    note: null,
    ...over,
  };
}

export function reversalRow(over: Record<string, unknown> = {}) {
  return payment({
    id: "55555555-0000-0000-0000-0000000000ff",
    amount: -5000,
    reversesId: PAYMENT_ID,
    note: "Ошибочный платёж",
    confirmedAtUtc: "2026-09-11T09:00:00Z",
    ...over,
  });
}

export function invoice(over: Record<string, unknown> = {}) {
  return {
    id: INVOICE_ID,
    number: "SI-2026-0007",
    studentId: STUDENT_ID,
    payerGuardianId: null,
    studyGroupId: GROUP_ID,
    periodFrom: "2026-09-01",
    periodTo: "2026-09-30",
    total: 7200,
    paidAmount: 0,
    currency: "RUB",
    status: "Issued",
    issuedOn: "2026-09-01",
    dueDate: "2026-09-15",
    isOverdue: false,
    comment: null,
    createdAtUtc: "2026-09-01T00:00:00Z",
    updatedAtUtc: null,
    ...over,
  };
}

export function invoiceDetail(over: Record<string, unknown> = {}) {
  const base = invoice(over);
  return {
    ...base,
    lines: [line()],
    payments: [],
    ...over,
  };
}

export function debtor(over: Record<string, unknown> = {}) {
  return {
    studentId: STUDENT_ID,
    debt: 7200,
    overdueInvoiceCount: 2,
    oldestDueDate: "2026-08-15",
    ...over,
  };
}

export function revenue(over: Record<string, unknown> = {}) {
  return {
    periodFrom: "2026-09-01",
    periodTo: "2026-09-30",
    total: 12000,
    byMethod: [
      { method: "Cash", amount: 8000 },
      { method: "Card", amount: 4000 },
    ],
    ...over,
  };
}

export function studentRow(id: string, name: string) {
  return {
    id,
    lastName: name.split(" ")[0] ?? name,
    firstName: name.split(" ")[1] ?? "",
    middleName: null,
    displayName: name,
    birthDate: "2010-01-01",
    phone: "",
    email: `${id}@acme.com`,
    userId: null,
    status: "Active",
    source: null,
    avatarFileId: null,
    managerUserId: "u-test-1",
    enrolledAtUtc: "2026-01-01T00:00:00Z",
  };
}

export function groupRow(over: Record<string, unknown> = {}) {
  return {
    id: GROUP_ID,
    code: "ENG-A1",
    name: "Английский A1",
    courseId: "c1",
    primaryTeacherId: "t1",
    format: "Offline",
    capacity: 8,
    activeEnrollmentCount: 2,
    startDate: "2026-02-01",
    endDate: null,
    status: "Active",
    chatChannelId: null,
    meetingUrl: null,
    roomId: null,
    notes: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    ...over,
  };
}

/** Reference-data mocks the Payments screens pull (students, groups,
 *  courses, tariffs). Register BEFORE page-specific mocks. */
export async function mockPaymentsRefs(page: Page) {
  await mockJsonResponse(
    page,
    "**/api/v1/students?**",
    paged([studentRow(STUDENT_ID, "Иванов Пётр"), studentRow(STUDENT_2, "Петров Иван")]),
  );
  await mockJsonResponse(page, "**/api/v1/study-groups?**", paged([groupRow()]));
  await mockJsonResponse(page, "**/api/v1/courses?**", paged([]));
  await mockJsonResponse(page, "**/api/v1/tariffs**", [tariff(), packageTariff()]);
}
