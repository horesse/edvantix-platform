import { apiFetch, ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";
import type { PagedResponse } from "@/api/people";

// ─────────────────────────────────────────────────────────────────────────
//  Payments API — tariffs, student invoices, payment confirmations,
//  balance and reports.
//
//  Hand-written types mirroring `Modules.Payments.Contracts` (no codegen,
//  see frontend/shared.md). Flat resource routing under `/api/v1` — no
//  `/payments` segment: `/tariffs`, `/student-invoices`, `/reports/...`.
//  Two cross-module routes keep the owning resource's name:
//  `/students/{id}/balance` and `/payments/{paymentId}/reverse`.
//  Backend reference: docs/02 Модули/Payments.md → "Контракты".
// ─────────────────────────────────────────────────────────────────────────

export type { PagedResponse };

/** `TariffKind` — how the tariff turns attendance into a charge. `PerPackage`
 *  is the only kind that uses `lessonsCount`/`validDays`. Serialised as a
 *  string by `JsonStringEnumConverter`. */
export type TariffKind = "PerLesson" | "PerMonth" | "PerPackage" | "OneTime";
/** Invoice lifecycle — `Status` is derived server-side from `Total`/`PaidAmount`,
 *  never set directly. */
export type InvoiceStatus =
  | "Draft"
  | "Issued"
  | "PartiallyPaid"
  | "Paid"
  | "Cancelled";
/** How a confirmed payment reached the school. */
export type PaymentMethod = "Cash" | "BankTransfer" | "Card" | "Online" | "Other";

export const TARIFF_KINDS: TariffKind[] = [
  "PerLesson",
  "PerMonth",
  "PerPackage",
  "OneTime",
];
export const INVOICE_STATUSES: InvoiceStatus[] = [
  "Draft",
  "Issued",
  "PartiallyPaid",
  "Paid",
  "Cancelled",
];
export const PAYMENT_METHODS: PaymentMethod[] = [
  "Cash",
  "BankTransfer",
  "Card",
  "Online",
  "Other",
];

// ─── DTOs ─────────────────────────────────────────────────────────────

export type TariffDto = {
  id: string;
  name: string;
  courseId?: string | null;
  kind: TariffKind;
  amount: number;
  currency: string;
  /** Package size — meaningful only for `kind === "PerPackage"`. */
  lessonsCount: number;
  /** Days the package stays valid; `0` means "never expires". `PerPackage` only. */
  validDays: number;
  chargeOnExcusedAbsence: boolean;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type InvoiceLineDto = {
  id: string;
  description: string;
  tariffId?: string | null;
  quantity: number;
  unitPrice: number;
  amount: number;
};

/** Write-side shape — the command replaces the whole line set in one call
 *  (`ReplaceLines` server-side, not a per-line PATCH). */
export type InvoiceLineInput = {
  description: string;
  tariffId?: string | null;
  quantity: number;
  unitPrice: number;
};

export type StudentInvoiceDto = {
  id: string;
  number: string;
  studentId: string;
  payerGuardianId?: string | null;
  studyGroupId?: string | null;
  /** `yyyy-MM-dd` (server `DateOnly`). */
  periodFrom: string;
  periodTo: string;
  total: number;
  paidAmount: number;
  currency: string;
  status: InvoiceStatus;
  issuedOn?: string | null;
  dueDate: string;
  /** Already computed server-side — do NOT recompute on the client. */
  isOverdue: boolean;
  comment?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type PaymentConfirmationDto = {
  id: string;
  invoiceId: string;
  /** Negative on a reversal row. */
  amount: number;
  paidOn: string;
  method: PaymentMethod;
  reference?: string | null;
  proofFileId?: string | null;
  confirmedByUserId: string;
  confirmedAtUtc: string;
  /** Set on a reversal row — points at the payment it stornoes. */
  reversesId?: string | null;
  note?: string | null;
};

/** `StudentInvoiceDto` plus its lines and payments — returned in one call. */
export type StudentInvoiceDetailDto = StudentInvoiceDto & {
  lines: InvoiceLineDto[];
  payments: PaymentConfirmationDto[];
};

/** Remaining-sessions projection for one `PerPackage` invoice. NOTE: per the
 *  Stage 5 backlog the package-remaining figure is not to be surfaced in the
 *  balance UI ("осталось N занятий") — kept here only for contract fidelity. */
export type PackageBalanceDto = {
  invoiceId: string;
  invoiceNumber: string;
  tariffId: string;
  tariffName: string;
  studyGroupId: string;
  lessonsCount: number;
  usedCount: number;
  remainingCount: number;
  issuedOn: string;
  expiresOn?: string | null;
  isExpired: boolean;
};

export type StudentBalanceDto = {
  studentId: string;
  charged: number;
  paid: number;
  debt: number;
  advance: number;
  overdueInvoices: StudentInvoiceDto[];
  packages: PackageBalanceDto[];
};

export type DebtorDto = {
  studentId: string;
  debt: number;
  overdueInvoiceCount: number;
  oldestDueDate: string;
};

export type RevenueByMethodDto = {
  method: PaymentMethod;
  amount: number;
};

export type RevenueReportDto = {
  periodFrom: string;
  periodTo: string;
  total: number;
  byMethod: RevenueByMethodDto[];
};

// ─── Tariffs ──────────────────────────────────────────────────────────

const TARIFFS = "/api/v1/tariffs";

export function getTariffs(isActive?: boolean | null): Promise<TariffDto[]> {
  const qs = isActive == null ? "" : `?isActive=${isActive ? "true" : "false"}`;
  return apiFetch<TariffDto[]>(`${TARIFFS}${qs}`);
}

export type CreateTariffInput = {
  name: string;
  courseId?: string | null;
  kind: TariffKind;
  amount: number;
  currency: string;
  lessonsCount: number;
  validDays: number;
  chargeOnExcusedAbsence: boolean;
};

export function createTariff(input: CreateTariffInput): Promise<string> {
  return apiFetch<string>(TARIFFS, {
    method: "POST",
    body: JSON.stringify({
      name: input.name.trim(),
      courseId: input.courseId ?? null,
      kind: input.kind,
      amount: input.amount,
      currency: input.currency.trim().toUpperCase(),
      lessonsCount: input.kind === "PerPackage" ? input.lessonsCount : 0,
      validDays: input.kind === "PerPackage" ? input.validDays : 0,
      chargeOnExcusedAbsence: input.chargeOnExcusedAbsence,
    }),
  });
}

/** `kind`/`currency` are immutable after creation — not part of this body. */
export type UpdateTariffInput = {
  name: string;
  courseId?: string | null;
  amount: number;
  lessonsCount: number;
  validDays: number;
  chargeOnExcusedAbsence: boolean;
};

export async function updateTariff(
  tariffId: string,
  input: UpdateTariffInput,
): Promise<void> {
  await apiFetch<void>(`${TARIFFS}/${encodeURIComponent(tariffId)}`, {
    method: "PUT",
    body: JSON.stringify({
      name: input.name.trim(),
      courseId: input.courseId ?? null,
      amount: input.amount,
      lessonsCount: input.lessonsCount,
      validDays: input.validDays,
      chargeOnExcusedAbsence: input.chargeOnExcusedAbsence,
    }),
  });
}

export async function deactivateTariff(tariffId: string): Promise<void> {
  await apiFetch<void>(
    `${TARIFFS}/${encodeURIComponent(tariffId)}/deactivate`,
    { method: "POST" },
  );
}

// ─── Student invoices ─────────────────────────────────────────────────

const INVOICES = "/api/v1/student-invoices";

export type SearchInvoicesParams = {
  studentId?: string | null;
  studyGroupId?: string | null;
  status?: InvoiceStatus | null;
  periodFrom?: string | null;
  periodTo?: string | null;
  hasDebt?: boolean | null;
  search?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string | null;
  sortDir?: "asc" | "desc" | null;
};

export function searchStudentInvoices(
  params: SearchInvoicesParams = {},
): Promise<PagedResponse<StudentInvoiceDto>> {
  const q = new URLSearchParams();
  if (params.studentId) q.set("studentId", params.studentId);
  if (params.studyGroupId) q.set("studyGroupId", params.studyGroupId);
  if (params.status) q.set("status", params.status);
  if (params.periodFrom) q.set("periodFrom", params.periodFrom);
  if (params.periodTo) q.set("periodTo", params.periodTo);
  if (params.hasDebt != null) q.set("hasDebt", params.hasDebt ? "true" : "false");
  if (params.search) q.set("search", params.search);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<StudentInvoiceDto>>(`${INVOICES}?${q.toString()}`);
}

export function getStudentInvoiceById(id: string): Promise<StudentInvoiceDetailDto> {
  return apiFetch<StudentInvoiceDetailDto>(`${INVOICES}/${encodeURIComponent(id)}`);
}

export type CreateStudentInvoiceInput = {
  studentId: string;
  payerGuardianId?: string | null;
  studyGroupId?: string | null;
  periodFrom: string;
  periodTo: string;
  dueDate: string;
  currency: string;
  comment?: string | null;
  lines: InvoiceLineInput[];
};

function serializeLines(lines: InvoiceLineInput[]) {
  return lines.map((l) => ({
    description: l.description.trim(),
    tariffId: l.tariffId ?? null,
    quantity: l.quantity,
    unitPrice: l.unitPrice,
  }));
}

export function createStudentInvoice(
  input: CreateStudentInvoiceInput,
): Promise<string> {
  return apiFetch<string>(INVOICES, {
    method: "POST",
    body: JSON.stringify({
      studentId: input.studentId,
      payerGuardianId: input.payerGuardianId ?? null,
      studyGroupId: input.studyGroupId ?? null,
      periodFrom: input.periodFrom,
      periodTo: input.periodTo,
      dueDate: input.dueDate,
      currency: input.currency.trim().toUpperCase(),
      comment: input.comment?.trim() || null,
      lines: serializeLines(input.lines),
    }),
  });
}

/** Draft-only. `currency` is not editable here — the server keeps the
 *  invoice's currency. Sends the WHOLE line set (ReplaceLines). */
export type UpdateStudentInvoiceInput = {
  payerGuardianId?: string | null;
  studyGroupId?: string | null;
  periodFrom: string;
  periodTo: string;
  dueDate: string;
  comment?: string | null;
  lines: InvoiceLineInput[];
};

export async function updateStudentInvoice(
  invoiceId: string,
  input: UpdateStudentInvoiceInput,
): Promise<void> {
  await apiFetch<void>(`${INVOICES}/${encodeURIComponent(invoiceId)}`, {
    method: "PUT",
    body: JSON.stringify({
      payerGuardianId: input.payerGuardianId ?? null,
      studyGroupId: input.studyGroupId ?? null,
      periodFrom: input.periodFrom,
      periodTo: input.periodTo,
      dueDate: input.dueDate,
      comment: input.comment?.trim() || null,
      lines: serializeLines(input.lines),
    }),
  });
}

/** Draft → Issued. Requires ≥1 line — server returns 409 on an empty draft. */
export async function issueInvoice(
  invoiceId: string,
  issuedOn: string,
): Promise<void> {
  await apiFetch<void>(`${INVOICES}/${encodeURIComponent(invoiceId)}/issue`, {
    method: "POST",
    body: JSON.stringify({ issuedOn }),
  });
}

/** Only when `paidAmount === 0` — otherwise reverse the payments first. */
export async function cancelInvoice(
  invoiceId: string,
  reason?: string | null,
): Promise<void> {
  await apiFetch<void>(`${INVOICES}/${encodeURIComponent(invoiceId)}/cancel`, {
    method: "POST",
    body: JSON.stringify({ reason: reason?.trim() || null }),
  });
}

/** Idempotent per (studyGroupId, periodFrom, periodTo) — a repeat call for the
 *  same group + period returns the existing invoice ids, no duplicates.
 *  `issueImmediately` defaults to `false`. Returns created + existing ids. */
export type BulkGenerateInput = {
  studyGroupId: string;
  periodFrom: string;
  periodTo: string;
  dueDate: string;
  issueImmediately?: boolean;
};

export function bulkGenerateInvoices(
  input: BulkGenerateInput,
): Promise<string[]> {
  return apiFetch<string[]>(`${INVOICES}/bulk-generate`, {
    method: "POST",
    body: JSON.stringify({
      studyGroupId: input.studyGroupId,
      periodFrom: input.periodFrom,
      periodTo: input.periodTo,
      dueDate: input.dueDate,
      issueImmediately: input.issueImmediately ?? false,
    }),
  });
}

/** Best-effort — the server silently skips ids that are not `Draft`. Returns
 *  the ids actually issued. */
export function bulkIssueInvoices(
  invoiceIds: string[],
  issuedOn: string,
): Promise<string[]> {
  return apiFetch<string[]>(`${INVOICES}/bulk-issue`, {
    method: "POST",
    body: JSON.stringify({ invoiceIds, issuedOn }),
  });
}

/** The caller's own invoices, or their wards' — resolved server-side via
 *  PeopleScope. */
export function getMyInvoices(
  status?: InvoiceStatus | null,
): Promise<StudentInvoiceDto[]> {
  const qs = status ? `?status=${encodeURIComponent(status)}` : "";
  return apiFetch<StudentInvoiceDto[]>(`${INVOICES}/my${qs}`);
}

/**
 * Stream an invoice PDF and trigger a browser download named `{number}.pdf`.
 * apiFetch only returns parsed JSON, so we fetch the blob directly here while
 * replicating apiFetch's auth + tenant headers (mirrors billing.ts).
 */
export async function downloadInvoicePdf(
  invoiceId: string,
  number: string,
): Promise<void> {
  const accessToken = tokenStore.getAccessToken();
  if (!accessToken) throw new ApiRequestError(401, "Not signed in");

  const headers = new Headers({ Authorization: `Bearer ${accessToken}` });
  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  if (tenant) headers.set("tenant", tenant);

  const response = await fetch(
    `${env.apiBase}/api/v1/student-invoices/${encodeURIComponent(invoiceId)}/pdf`,
    { headers },
  );
  if (!response.ok) {
    throw new ApiRequestError(
      response.status,
      `Failed to download invoice (${response.status})`,
    );
  }

  const blob = await response.blob();
  const objectUrl = window.URL.createObjectURL(blob);
  try {
    const anchor = document.createElement("a");
    anchor.href = objectUrl;
    anchor.download = `${number}.pdf`;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    window.URL.revokeObjectURL(objectUrl);
  }
}

// ─── Payments (against an invoice) ────────────────────────────────────

export function getInvoicePayments(
  invoiceId: string,
): Promise<PaymentConfirmationDto[]> {
  return apiFetch<PaymentConfirmationDto[]>(
    `${INVOICES}/${encodeURIComponent(invoiceId)}/payments`,
  );
}

export type ConfirmPaymentInput = {
  amount: number;
  paidOn: string;
  method: PaymentMethod;
  reference?: string | null;
  proofFileId?: string | null;
  note?: string | null;
};

/** Records a manager-confirmed payment. Overpayment is allowed — do NOT clamp
 *  `amount` to the outstanding balance. */
export function confirmPayment(
  invoiceId: string,
  input: ConfirmPaymentInput,
): Promise<string> {
  return apiFetch<string>(
    `${INVOICES}/${encodeURIComponent(invoiceId)}/payments`,
    {
      method: "POST",
      body: JSON.stringify({
        amount: input.amount,
        paidOn: input.paidOn,
        method: input.method,
        reference: input.reference?.trim() || null,
        proofFileId: input.proofFileId ?? null,
        note: input.note?.trim() || null,
      }),
    },
  );
}

/** Reverse a confirmed payment. A reason note is required (server-side too).
 *  The reversal appears in the same `payments[]` list with a negative amount
 *  and `reversesId` set. */
export function reversePayment(
  paymentId: string,
  note: string,
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/payments/${encodeURIComponent(paymentId)}/reverse`,
    {
      method: "POST",
      body: JSON.stringify({ note: note.trim() }),
    },
  );
}

// ─── Balance & reports ───────────────────────────────────────────────

export function getStudentBalance(studentId: string): Promise<StudentBalanceDto> {
  return apiFetch<StudentBalanceDto>(
    `/api/v1/students/${encodeURIComponent(studentId)}/balance`,
  );
}

export function getDebtorsReport(
  studyGroupId?: string | null,
): Promise<DebtorDto[]> {
  const qs = studyGroupId ? `?studyGroupId=${encodeURIComponent(studyGroupId)}` : "";
  return apiFetch<DebtorDto[]>(`/api/v1/reports/debtors${qs}`);
}

export function getRevenueReport(
  periodFrom: string,
  periodTo: string,
): Promise<RevenueReportDto> {
  const q = new URLSearchParams({ periodFrom, periodTo });
  return apiFetch<RevenueReportDto>(`/api/v1/reports/revenue?${q.toString()}`);
}

// ─── Client-side helpers ─────────────────────────────────────────────

/** Outstanding = total − paid, floored at 0 (overpayment shows as advance). */
export function outstanding(inv: Pick<StudentInvoiceDto, "total" | "paidAmount">): number {
  return Math.max(0, round2(inv.total - inv.paidAmount));
}

/** Advance = paid − total, floored at 0. */
export function advance(inv: Pick<StudentInvoiceDto, "total" | "paidAmount">): number {
  return Math.max(0, round2(inv.paidAmount - inv.total));
}

export function lineAmount(l: Pick<InvoiceLineInput, "quantity" | "unitPrice">): number {
  return round2((Number(l.quantity) || 0) * (Number(l.unitPrice) || 0));
}

function round2(n: number): number {
  return Math.round((n + Number.EPSILON) * 100) / 100;
}
