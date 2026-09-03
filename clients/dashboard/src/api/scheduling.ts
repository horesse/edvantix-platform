import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/people";

// ─────────────────────────────────────────────────────────────────────────
//  Scheduling API — sessions, calendar, schedule templates, rooms,
//  non-working days, attendance.
//
//  Hand-written types mirroring `Modules.Scheduling.Contracts` (there is
//  no codegen step, see frontend/shared.md). Flat resource routing under
//  `/api/v1` — no `/scheduling` segment. Two cross-module routes keep the
//  owning resource's name: `/students/{id}/attendance` and
//  `/study-groups/{id}/{schedule-templates,attendance-report}`.
//  Backend reference: docs/02 Модули/Scheduling.md → "Контракты"/"DTO".
// ─────────────────────────────────────────────────────────────────────────

export type { PagedResponse };

/** Session lifecycle: `Planned → Held`, or `Planned → Cancelled/Rescheduled`. */
export type SessionStatus = "Planned" | "Held" | "Cancelled" | "Rescheduled";
/** Per-student attendance mark. Server default for a freshly-seeded row is `Present`. */
export type AttendanceStatus = "Present" | "Absent" | "Late" | "Excused";
/** Which resource a candidate slot clashed on. */
export type SessionConflictType = "Teacher" | "Room" | "StudyGroup";
/** Why the generator skipped an occurrence. */
export type GenerationSkipReason = "NonWorkingDay" | "Conflict";
/** `System.DayOfWeek` — serialised as a string by `JsonStringEnumConverter`. */
export type DayOfWeekName =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

export const SESSION_STATUSES: SessionStatus[] = [
  "Planned",
  "Held",
  "Cancelled",
  "Rescheduled",
];
export const ATTENDANCE_STATUSES: AttendanceStatus[] = [
  "Present",
  "Absent",
  "Late",
  "Excused",
];
export const DAYS_OF_WEEK: DayOfWeekName[] = [
  "Monday",
  "Tuesday",
  "Wednesday",
  "Thursday",
  "Friday",
  "Saturday",
  "Sunday",
];

// ─── DTOs ─────────────────────────────────────────────────────────────

/** One block on the calendar view — `SessionDto` minus the bookkeeping-only
 *  fields. `startUtc`/`endUtc` are ISO strings in UTC; convert to the school
 *  timezone on the client for display only. */
export type CalendarEntryDto = {
  sessionId: string;
  studyGroupId: string;
  teacherId: string;
  roomId?: string | null;
  startUtc: string;
  endUtc: string;
  status: SessionStatus;
  topic?: string | null;
};

export type SessionDto = {
  id: string;
  studyGroupId: string;
  lessonId?: string | null;
  teacherId: string;
  roomId?: string | null;
  startUtc: string;
  endUtc: string;
  status: SessionStatus;
  topic?: string | null;
  meetingUrl?: string | null;
  scheduleTemplateId?: string | null;
  rescheduledFromId?: string | null;
};

export type AttendanceDto = {
  id: string;
  sessionId: string;
  studentId: string;
  status: AttendanceStatus;
  comment?: string | null;
  markedByUserId?: string | null;
  markedAtUtc: string;
};

/** `resolvedTopic` is already computed server-side (`Session.Topic` override,
 *  or the linked lesson's title when empty — ADR-006). Lesson materials are
 *  NOT included here — fetch them from Curriculum when `lessonId` is set. */
export type SessionDetailDto = {
  id: string;
  studyGroupId: string;
  lessonId?: string | null;
  teacherId: string;
  roomId?: string | null;
  startUtc: string;
  endUtc: string;
  status: SessionStatus;
  resolvedTopic: string;
  meetingUrl?: string | null;
  cancelReason?: string | null;
  rescheduledFromId?: string | null;
  scheduleTemplateId?: string | null;
  teacherComment?: string | null;
  attendance: AttendanceDto[];
};

export type ScheduleTemplateDto = {
  id: string;
  studyGroupId: string;
  dayOfWeek: DayOfWeekName;
  /** Local wall-clock start, `"HH:mm:ss"` (server `TimeOnly`). */
  startTime: string;
  durationMinutes: number;
  roomId?: string | null;
  /** Empty means "use the group's `primaryTeacherId`" (resolved server-side). */
  teacherId?: string | null;
  /** `yyyy-MM-dd` (server `DateOnly`). */
  validFrom: string;
  validTo?: string | null;
  isActive: boolean;
};

export type RoomDto = {
  id: string;
  name: string;
  capacity: number;
  location?: string | null;
  /** Virtual rooms are excluded from the session conflict check. */
  isVirtual: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type NonWorkingDayDto = {
  id: string;
  /** `yyyy-MM-dd` (server `DateOnly`). */
  date: string;
  description?: string | null;
};

/** One resource clash reported by the generator preview / a manual 409. */
export type SessionConflictDto = {
  type: SessionConflictType;
  conflictingSessionId: string;
  conflictingSessionStartUtc: string;
};

export type GeneratedSessionPreviewDto = {
  /** `yyyy-MM-dd` local date of the occurrence. */
  localDate: string;
  startUtc: string;
  endUtc: string;
};

export type GenerationSkipDto = {
  localDate: string;
  reason: GenerationSkipReason;
  /** Populated only when `reason === "Conflict"`. */
  conflicts: SessionConflictDto[];
};

/** What `generate` *would* do — computed without writing anything. */
export type GenerationPreviewDto = {
  scheduleTemplateId: string;
  toCreate: GeneratedSessionPreviewDto[];
  skipped: GenerationSkipDto[];
};

/** What `generate` actually did. */
export type GenerationResultDto = {
  scheduleTemplateId: string;
  createdSessionIds: string[];
  skipped: GenerationSkipDto[];
};

export type StudentAttendanceSummaryDto = {
  studentId: string;
  presentCount: number;
  absentCount: number;
  lateCount: number;
  excusedCount: number;
  totalCount: number;
};

export type AttendanceReportDto = {
  studyGroupId: string;
  from: string;
  to: string;
  students: StudentAttendanceSummaryDto[];
};

/** A teacher's group/session workload for a period — shown on the teacher
 *  profile card. Computed in Scheduling (not People): needs StudyGroups +
 *  Session rows. `from`/`to` are `yyyy-MM-dd`. */
export type TeacherWorkloadDto = {
  teacherId: string;
  from: string;
  to: string;
  activeGroupsCount: number;
  sessionsCount: number;
  totalHours: number;
};

// ─── Calendar & sessions ──────────────────────────────────────────────

const SESSIONS = "/api/v1/sessions";
const TEMPLATES = "/api/v1/schedule-templates";
const ROOMS = "/api/v1/rooms";
const NON_WORKING_DAYS = "/api/v1/non-working-days";

export type CalendarParams = {
  /** ISO instant — inclusive lower bound on `startUtc`. */
  from: string;
  /** ISO instant — inclusive upper bound on `startUtc`. */
  to: string;
  studyGroupId?: string | null;
  teacherId?: string | null;
  roomId?: string | null;
};

export function getCalendar(params: CalendarParams): Promise<CalendarEntryDto[]> {
  const q = new URLSearchParams({ from: params.from, to: params.to });
  if (params.studyGroupId) q.set("studyGroupId", params.studyGroupId);
  if (params.teacherId) q.set("teacherId", params.teacherId);
  if (params.roomId) q.set("roomId", params.roomId);
  return apiFetch<CalendarEntryDto[]>(`${SESSIONS}/calendar?${q.toString()}`);
}

export function getSessionById(id: string): Promise<SessionDetailDto> {
  return apiFetch<SessionDetailDto>(`${SESSIONS}/${encodeURIComponent(id)}`);
}

export type SearchSessionsParams = {
  studyGroupId?: string | null;
  teacherId?: string | null;
  roomId?: string | null;
  from?: string | null;
  to?: string | null;
  status?: SessionStatus | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export function searchSessions(
  params: SearchSessionsParams = {},
): Promise<PagedResponse<SessionDto>> {
  const q = new URLSearchParams();
  if (params.studyGroupId) q.set("studyGroupId", params.studyGroupId);
  if (params.teacherId) q.set("teacherId", params.teacherId);
  if (params.roomId) q.set("roomId", params.roomId);
  if (params.from) q.set("from", params.from);
  if (params.to) q.set("to", params.to);
  if (params.status) q.set("status", params.status);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 50));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<SessionDto>>(`${SESSIONS}?${q.toString()}`);
}

/** "My schedule" — teacher/student/guardian, resolved server-side via PeopleScope.
 *
 *  `studentId` — narrowing hint the guardian cabinet passes when a specific
 *  ward is selected in the ward switcher. The server currently returns the
 *  union of the caller's (and all their wards') groups' sessions; the param
 *  is forward-compatible and also keys the client cache per selected ward. */
export function getMySchedule(
  from: string,
  to: string,
  studentId?: string | null,
): Promise<SessionDto[]> {
  const q = new URLSearchParams({ from, to });
  if (studentId) q.set("studentId", studentId);
  return apiFetch<SessionDto[]>(`${SESSIONS}/my?${q.toString()}`);
}

/** Planned → Held. Seeds one attendance row per active student server-side —
 *  the caller must re-fetch attendance after this resolves. */
export async function holdSession(id: string): Promise<void> {
  await apiFetch<void>(`${SESSIONS}/${encodeURIComponent(id)}/hold`, {
    method: "POST",
  });
}

/** Planned → Cancelled, with an optional reason. */
export async function cancelSession(id: string, reason?: string | null): Promise<void> {
  await apiFetch<void>(`${SESSIONS}/${encodeURIComponent(id)}/cancel`, {
    method: "POST",
    body: JSON.stringify({ reason: reason?.trim() || null }),
  });
}

export type RescheduleSessionInput = {
  sessionId: string;
  newStartUtc: string;
  newEndUtc: string;
  roomId?: string | null;
  teacherId?: string | null;
  /** `false` → the server returns 409 on a resource clash (surface the
   *  description, then retry with `force: true`). */
  force?: boolean;
};

/** Marks the current session `Rescheduled` and creates a replacement. Returns
 *  the new session's id. 409 on conflict unless `force`. */
export function rescheduleSession(input: RescheduleSessionInput): Promise<string> {
  return apiFetch<string>(
    `${SESSIONS}/${encodeURIComponent(input.sessionId)}/reschedule`,
    {
      method: "POST",
      body: JSON.stringify({
        newStartUtc: input.newStartUtc,
        newEndUtc: input.newEndUtc,
        roomId: input.roomId ?? null,
        teacherId: input.teacherId ?? null,
        force: input.force ?? false,
      }),
    },
  );
}

// ─── Attendance ───────────────────────────────────────────────────────

export function getSessionAttendance(sessionId: string): Promise<AttendanceDto[]> {
  return apiFetch<AttendanceDto[]>(
    `${SESSIONS}/${encodeURIComponent(sessionId)}/attendance`,
  );
}

export type AttendanceMarkInput = {
  studentId: string;
  status: AttendanceStatus;
  comment?: string | null;
};

/** Bulk mark — one request covers the whole session roster. Rows not sent keep
 *  their server-side value; typical use marks only the exceptions. */
export async function markAttendance(
  sessionId: string,
  marks: AttendanceMarkInput[],
): Promise<void> {
  await apiFetch<void>(`${SESSIONS}/${encodeURIComponent(sessionId)}/attendance`, {
    method: "PUT",
    body: JSON.stringify(
      marks.map((m) => ({
        studentId: m.studentId,
        status: m.status,
        comment: m.comment?.trim() || null,
      })),
    ),
  });
}

export function getStudentAttendance(
  studentId: string,
  from?: string | null,
  to?: string | null,
): Promise<AttendanceDto[]> {
  const q = new URLSearchParams();
  if (from) q.set("from", from);
  if (to) q.set("to", to);
  const qs = q.toString();
  return apiFetch<AttendanceDto[]>(
    `/api/v1/students/${encodeURIComponent(studentId)}/attendance${qs ? `?${qs}` : ""}`,
  );
}

export function getGroupAttendanceReport(
  studyGroupId: string,
  from: string,
  to: string,
): Promise<AttendanceReportDto> {
  const q = new URLSearchParams({ from, to });
  return apiFetch<AttendanceReportDto>(
    `/api/v1/study-groups/${encodeURIComponent(studyGroupId)}/attendance-report?${q.toString()}`,
  );
}

// ─── Teacher workload ─────────────────────────────────────────────────

/** `GET /teachers/{id}/workload` — cross-module route mapped by Scheduling
 *  under People's resource name. Gated by `Scheduling.Sessions.View`. Omit
 *  `to` and the server defaults to a 7-day window ahead of `from`. */
export function getTeacherWorkload(
  teacherId: string,
  params: { from?: string | null; to?: string | null } = {},
): Promise<TeacherWorkloadDto> {
  const q = new URLSearchParams();
  if (params.from) q.set("from", params.from);
  if (params.to) q.set("to", params.to);
  const qs = q.toString();
  return apiFetch<TeacherWorkloadDto>(
    `/api/v1/teachers/${encodeURIComponent(teacherId)}/workload${qs ? `?${qs}` : ""}`,
  );
}

// ─── Schedule templates ───────────────────────────────────────────────

export function getScheduleTemplates(
  studyGroupId: string,
): Promise<ScheduleTemplateDto[]> {
  return apiFetch<ScheduleTemplateDto[]>(
    `/api/v1/study-groups/${encodeURIComponent(studyGroupId)}/schedule-templates`,
  );
}

export type CreateScheduleTemplateInput = {
  studyGroupId: string;
  dayOfWeek: DayOfWeekName;
  /** `"HH:mm"` or `"HH:mm:ss"` — normalised to `"HH:mm:ss"` before sending. */
  startTime: string;
  durationMinutes: number;
  roomId?: string | null;
  teacherId?: string | null;
  validFrom: string;
  validTo?: string | null;
};

function normalizeTime(t: string): string {
  return t.length === 5 ? `${t}:00` : t;
}

export function createScheduleTemplate(
  input: CreateScheduleTemplateInput,
): Promise<string> {
  return apiFetch<string>(
    `/api/v1/study-groups/${encodeURIComponent(input.studyGroupId)}/schedule-templates`,
    {
      method: "POST",
      body: JSON.stringify({
        dayOfWeek: input.dayOfWeek,
        startTime: normalizeTime(input.startTime),
        durationMinutes: input.durationMinutes,
        roomId: input.roomId ?? null,
        teacherId: input.teacherId ?? null,
        validFrom: input.validFrom,
        validTo: input.validTo ?? null,
      }),
    },
  );
}

export type UpdateScheduleTemplateInput = CreateScheduleTemplateInput & {
  scheduleTemplateId: string;
  isActive: boolean;
};

export async function updateScheduleTemplate(
  input: UpdateScheduleTemplateInput,
): Promise<void> {
  await apiFetch<void>(
    `${TEMPLATES}/${encodeURIComponent(input.scheduleTemplateId)}`,
    {
      method: "PUT",
      body: JSON.stringify({
        dayOfWeek: input.dayOfWeek,
        startTime: normalizeTime(input.startTime),
        durationMinutes: input.durationMinutes,
        roomId: input.roomId ?? null,
        teacherId: input.teacherId ?? null,
        validFrom: input.validFrom,
        validTo: input.validTo ?? null,
        isActive: input.isActive,
      }),
    },
  );
}

export async function deleteScheduleTemplate(id: string): Promise<void> {
  await apiFetch<void>(`${TEMPLATES}/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

/** Preview what generating from a template would create — writes nothing.
 *  `horizonWeeks` defaults to 8 server-side. Needs `Sessions.Generate`. */
export function previewGeneration(
  scheduleTemplateId: string,
  horizonWeeks?: number | null,
): Promise<GenerationPreviewDto> {
  const q = new URLSearchParams();
  if (horizonWeeks != null) q.set("horizonWeeks", String(horizonWeeks));
  const qs = q.toString();
  return apiFetch<GenerationPreviewDto>(
    `${TEMPLATES}/${encodeURIComponent(scheduleTemplateId)}/preview${qs ? `?${qs}` : ""}`,
    { method: "POST" },
  );
}

/** Apply the template — mass create. Gated by `Sessions.Generate`, separate
 *  from `Sessions.Create` on purpose. SignalR does NOT fan out per created
 *  session, so the caller must invalidate the session list itself. */
export function generateSessions(
  scheduleTemplateId: string,
  horizonWeeks?: number | null,
): Promise<GenerationResultDto> {
  const q = new URLSearchParams();
  if (horizonWeeks != null) q.set("horizonWeeks", String(horizonWeeks));
  const qs = q.toString();
  return apiFetch<GenerationResultDto>(
    `${TEMPLATES}/${encodeURIComponent(scheduleTemplateId)}/generate${qs ? `?${qs}` : ""}`,
    { method: "POST" },
  );
}

// ─── Rooms ────────────────────────────────────────────────────────────

export function getRooms(): Promise<RoomDto[]> {
  return apiFetch<RoomDto[]>(ROOMS);
}

export type RoomInput = {
  name: string;
  capacity: number;
  location?: string | null;
  isVirtual: boolean;
};

export function createRoom(input: RoomInput): Promise<string> {
  return apiFetch<string>(ROOMS, {
    method: "POST",
    body: JSON.stringify({
      name: input.name.trim(),
      capacity: input.capacity,
      location: input.location?.trim() || null,
      isVirtual: input.isVirtual,
    }),
  });
}

export async function updateRoom(
  roomId: string,
  input: RoomInput,
): Promise<void> {
  await apiFetch<void>(`${ROOMS}/${encodeURIComponent(roomId)}`, {
    method: "PUT",
    body: JSON.stringify({
      name: input.name.trim(),
      capacity: input.capacity,
      location: input.location?.trim() || null,
      isVirtual: input.isVirtual,
    }),
  });
}

export async function deleteRoom(roomId: string): Promise<void> {
  await apiFetch<void>(`${ROOMS}/${encodeURIComponent(roomId)}`, {
    method: "DELETE",
  });
}

// ─── Non-working days ─────────────────────────────────────────────────

export function getNonWorkingDays(
  from?: string | null,
  to?: string | null,
): Promise<NonWorkingDayDto[]> {
  const q = new URLSearchParams();
  if (from) q.set("from", from);
  if (to) q.set("to", to);
  const qs = q.toString();
  return apiFetch<NonWorkingDayDto[]>(`${NON_WORKING_DAYS}${qs ? `?${qs}` : ""}`);
}

export function addNonWorkingDay(input: {
  date: string;
  description?: string | null;
}): Promise<string> {
  return apiFetch<string>(NON_WORKING_DAYS, {
    method: "POST",
    body: JSON.stringify({
      date: input.date,
      description: input.description?.trim() || null,
    }),
  });
}

export async function removeNonWorkingDay(id: string): Promise<void> {
  await apiFetch<void>(`${NON_WORKING_DAYS}/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}
