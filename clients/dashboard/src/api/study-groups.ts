import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/people";

// ─────────────────────────────────────────────────────────────────────────
//  StudyGroups API — study groups, enrollments, teacher roster.
//
//  Hand-written types mirroring `Modules.StudyGroups.Contracts` (there is
//  no codegen step, see frontend/shared.md). Flat resource routing under
//  `/api/v1` — no `/study-groups` segment beyond the resource name.
//  Backend reference: docs/02 Модули/StudyGroups.md → "Контракты".
// ─────────────────────────────────────────────────────────────────────────

export type { PagedResponse };

/** Group delivery mode. */
export type GroupFormat = "Online" | "Offline" | "Hybrid";
/** Lifecycle: `Forming → Active → Finished`, or `Forming/Active → Cancelled`. */
export type StudyGroupStatus = "Forming" | "Active" | "Finished" | "Cancelled";
/** Enrollment lifecycle: `Active ↔ Paused`, `Active/Paused → Left`, `→ Completed`. */
export type EnrollmentStatus = "Active" | "Paused" | "Left" | "Completed";
/** Roster role — independent of the group's denormalized `primaryTeacherId`. */
export type TeacherRole = "Primary" | "Assistant" | "Substitute";

export const GROUP_FORMATS: GroupFormat[] = ["Online", "Offline", "Hybrid"];
export const STUDY_GROUP_STATUSES: StudyGroupStatus[] = [
  "Forming",
  "Active",
  "Finished",
  "Cancelled",
];
export const TEACHER_ROLES: TeacherRole[] = ["Primary", "Assistant", "Substitute"];

// ─── DTOs ─────────────────────────────────────────────────────────────

export type StudyGroupDto = {
  id: string;
  /** Stable business key — immutable after creation. */
  code: string;
  name: string;
  courseId: string;
  primaryTeacherId: string;
  format: GroupFormat;
  capacity: number;
  activeEnrollmentCount: number;
  /** `yyyy-MM-dd` (server `DateOnly`). */
  startDate: string;
  endDate?: string | null;
  status: StudyGroupStatus;
  chatChannelId?: string | null;
  meetingUrl?: string | null;
  roomId?: string | null;
  notes?: string | null;
  createdAtUtc: string;
};

export type GroupEnrollmentDto = {
  id: string;
  studyGroupId: string;
  studentId: string;
  enrolledOn: string;
  leftOn?: string | null;
  status: EnrollmentStatus;
  leaveReason?: string | null;
  tariffId?: string | null;
  discountPercent: number;
};

export type GroupTeacherDto = {
  id: string;
  studyGroupId: string;
  teacherId: string;
  role: TeacherRole;
};

/** `StudyGroupDto` plus the roster — `GET /study-groups/{id}` returns it
 *  with `enrollments[]`/`teachers[]` already composed, so the builder
 *  never fetches the roster separately. */
export type StudyGroupDetailDto = StudyGroupDto & {
  enrollments: GroupEnrollmentDto[];
  teachers: GroupTeacherDto[];
};

// ─── Study groups ─────────────────────────────────────────────────────

export type SearchStudyGroupsParams = {
  search?: string;
  courseId?: string | null;
  teacherId?: string | null;
  status?: StudyGroupStatus | null;
  format?: GroupFormat | null;
  pageNumber?: number;
  pageSize?: number;
  /** One of `code | name | startDate | status` (default `code`). */
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

const BASE = "/api/v1/study-groups";
const ENROLLMENTS = "/api/v1/enrollments";

export function searchStudyGroups(
  params: SearchStudyGroupsParams = {},
): Promise<PagedResponse<StudyGroupDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.courseId) q.set("courseId", params.courseId);
  if (params.teacherId) q.set("teacherId", params.teacherId);
  if (params.status) q.set("status", params.status);
  if (params.format) q.set("format", params.format);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<StudyGroupDto>>(`${BASE}?${q.toString()}`);
}

export function getStudyGroupById(id: string): Promise<StudyGroupDetailDto> {
  return apiFetch<StudyGroupDetailDto>(`${BASE}/${encodeURIComponent(id)}`);
}

/** "My groups" — as teacher or student, resolved server-side via PeopleScope. */
export function getMyStudyGroups(): Promise<StudyGroupDto[]> {
  return apiFetch<StudyGroupDto[]>(`${BASE}/my`);
}

export type CreateStudyGroupInput = {
  code: string;
  name: string;
  courseId: string;
  primaryTeacherId: string;
  format: GroupFormat;
  capacity: number;
  startDate: string;
  endDate?: string | null;
  meetingUrl?: string | null;
  roomId?: string | null;
  notes?: string | null;
};

export function createStudyGroup(input: CreateStudyGroupInput): Promise<string> {
  return apiFetch<string>(BASE, {
    method: "POST",
    body: JSON.stringify({
      code: input.code,
      name: input.name,
      courseId: input.courseId,
      primaryTeacherId: input.primaryTeacherId,
      format: input.format,
      capacity: input.capacity,
      startDate: input.startDate,
      endDate: input.endDate ?? null,
      meetingUrl: input.meetingUrl ?? null,
      roomId: input.roomId ?? null,
      notes: input.notes ?? null,
    }),
  });
}

/** `code` is not updatable — the server ignores it, so it is not sent. */
export type UpdateStudyGroupInput = {
  studyGroupId: string;
  name: string;
  primaryTeacherId: string;
  format: GroupFormat;
  capacity: number;
  startDate: string;
  endDate?: string | null;
  meetingUrl?: string | null;
  roomId?: string | null;
  notes?: string | null;
};

export async function updateStudyGroup(input: UpdateStudyGroupInput): Promise<void> {
  await apiFetch<void>(`${BASE}/${encodeURIComponent(input.studyGroupId)}`, {
    method: "PUT",
    body: JSON.stringify({
      name: input.name,
      primaryTeacherId: input.primaryTeacherId,
      format: input.format,
      capacity: input.capacity,
      startDate: input.startDate,
      endDate: input.endDate ?? null,
      meetingUrl: input.meetingUrl ?? null,
      roomId: input.roomId ?? null,
      notes: input.notes ?? null,
    }),
  });
}

export async function deleteStudyGroup(id: string): Promise<void> {
  await apiFetch<void>(`${BASE}/${encodeURIComponent(id)}`, { method: "DELETE" });
}

// ── Lifecycle (all gated by StudyGroups.Archive) ──

/** Forming → Active. 409 if the group has no enrollments — surface the reason. */
export async function activateStudyGroup(id: string): Promise<void> {
  await apiFetch<void>(`${BASE}/${encodeURIComponent(id)}/activate`, { method: "POST" });
}

/** Active → Finished. Freezes the roster. */
export async function finishStudyGroup(id: string): Promise<void> {
  await apiFetch<void>(`${BASE}/${encodeURIComponent(id)}/finish`, { method: "POST" });
}

/** Forming/Active → Cancelled, with an optional reason. */
export async function cancelStudyGroup(id: string, reason?: string | null): Promise<void> {
  await apiFetch<void>(`${BASE}/${encodeURIComponent(id)}/cancel`, {
    method: "POST",
    body: JSON.stringify({ reason: reason?.trim() || null }),
  });
}

// ── Teacher roster (add/remove gated by StudyGroups.Update) ──

export function addGroupTeacher(input: {
  studyGroupId: string;
  teacherId: string;
  role: TeacherRole;
}): Promise<string> {
  return apiFetch<string>(
    `${BASE}/${encodeURIComponent(input.studyGroupId)}/teachers`,
    {
      method: "POST",
      body: JSON.stringify({ teacherId: input.teacherId, role: input.role }),
    },
  );
}

/** Removal keys on `teacherId`, not the roster row id. */
export async function removeGroupTeacher(
  studyGroupId: string,
  teacherId: string,
): Promise<void> {
  await apiFetch<void>(
    `${BASE}/${encodeURIComponent(studyGroupId)}/teachers/${encodeURIComponent(teacherId)}`,
    { method: "DELETE" },
  );
}

// ── Enrollments ──

export function getGroupEnrollments(
  studyGroupId: string,
): Promise<GroupEnrollmentDto[]> {
  return apiFetch<GroupEnrollmentDto[]>(
    `${BASE}/${encodeURIComponent(studyGroupId)}/enrollments`,
  );
}

/** A student's full enrollment history, including finished/left groups. */
export function getStudentEnrollments(
  studentId: string,
): Promise<GroupEnrollmentDto[]> {
  return apiFetch<GroupEnrollmentDto[]>(
    `/api/v1/students/${encodeURIComponent(studentId)}/enrollments`,
  );
}

export type EnrollStudentsInput = {
  studyGroupId: string;
  studentIds: string[];
  enrolledOn?: string | null;
  tariffId?: string | null;
  discountPercent?: number;
};

/** Enrolls one or more students in one call. The whole batch is rejected
 *  with 409 if it would exceed `capacity` — surface that as "мест нет". */
export function enrollStudents(input: EnrollStudentsInput): Promise<string[]> {
  return apiFetch<string[]>(
    `${BASE}/${encodeURIComponent(input.studyGroupId)}/enrollments`,
    {
      method: "POST",
      body: JSON.stringify({
        studentIds: input.studentIds,
        enrolledOn: input.enrolledOn ?? null,
        tariffId: input.tariffId ?? null,
        discountPercent: input.discountPercent ?? 0,
      }),
    },
  );
}

/** Marks the enrollment `Left` with a reason (never deletes the row). The
 *  endpoint only binds `reason` (a query param) — `leftOn` is server-set. */
export async function unenrollStudent(input: {
  studyGroupId: string;
  enrollmentId: string;
  reason?: string | null;
}): Promise<void> {
  const q = new URLSearchParams();
  if (input.reason?.trim()) q.set("reason", input.reason.trim());
  const qs = q.toString();
  await apiFetch<void>(
    `${BASE}/${encodeURIComponent(input.studyGroupId)}/enrollments/${encodeURIComponent(
      input.enrollmentId,
    )}${qs ? `?${qs}` : ""}`,
    { method: "DELETE" },
  );
}

/** Atomically closes the source enrollment (`Left`, reason "Transfer") and
 *  opens a new one in the target group — tariff/discount carry over. */
export function transferEnrollment(input: {
  enrollmentId: string;
  targetStudyGroupId: string;
  transferDate?: string | null;
}): Promise<string> {
  return apiFetch<string>(
    `${ENROLLMENTS}/${encodeURIComponent(input.enrollmentId)}/transfer`,
    {
      method: "POST",
      body: JSON.stringify({
        targetStudyGroupId: input.targetStudyGroupId,
        transferDate: input.transferDate ?? null,
      }),
    },
  );
}

/** Pause / resume — quick roster actions, both gated by Enrollments.Create. */
export async function pauseEnrollment(enrollmentId: string): Promise<void> {
  await apiFetch<void>(
    `${ENROLLMENTS}/${encodeURIComponent(enrollmentId)}/pause`,
    { method: "POST" },
  );
}

export async function resumeEnrollment(enrollmentId: string): Promise<void> {
  await apiFetch<void>(
    `${ENROLLMENTS}/${encodeURIComponent(enrollmentId)}/resume`,
    { method: "POST" },
  );
}

/** Re-prices a live enrollment in place (no re-enrolment). Past invoices are
 *  left as-is; the new terms apply from the next bulk generation. Gated by
 *  `Enrollments.Update`. `tariffId: null` falls back to the course tariff. */
export async function changeEnrollmentTariff(input: {
  enrollmentId: string;
  tariffId: string | null;
  discountPercent: number;
}): Promise<void> {
  await apiFetch<void>(
    `${ENROLLMENTS}/${encodeURIComponent(input.enrollmentId)}/tariff`,
    {
      method: "PUT",
      body: JSON.stringify({
        tariffId: input.tariffId ?? null,
        discountPercent: input.discountPercent,
      }),
    },
  );
}
