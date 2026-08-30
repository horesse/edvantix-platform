import type { EntityStatusTone } from "@/components/list";
import type {
  AttendanceStatus,
  DayOfWeekName,
  GenerationSkipReason,
  SessionConflictType,
  SessionStatus,
} from "@/api/scheduling";
import { ApiRequestError } from "@/lib/api-client";
import { describe } from "@/lib/list-helpers";

// Shared display maps for the Scheduling screens. Component-free module so
// importing it doesn't trip react-refresh (same pattern as study-groups-ui.ts).

export const SESSION_STATUS_LABEL: Record<SessionStatus, string> = {
  Planned: "Запланировано",
  Held: "Проведено",
  Cancelled: "Отменено",
  Rescheduled: "Перенесено",
};

export const SESSION_STATUS_TONE: Record<SessionStatus, EntityStatusTone> = {
  Planned: "info",
  Held: "success",
  Cancelled: "danger",
  Rescheduled: "warning",
};

export const ATTENDANCE_STATUS_LABEL: Record<AttendanceStatus, string> = {
  Present: "Был",
  Absent: "Не был",
  Late: "Опоздал",
  Excused: "Уваж.",
};

export const ATTENDANCE_STATUS_TONE: Record<AttendanceStatus, EntityStatusTone> = {
  Present: "success",
  Absent: "danger",
  Late: "warning",
  Excused: "info",
};

export const CONFLICT_TYPE_LABEL: Record<SessionConflictType, string> = {
  Teacher: "Преподаватель занят",
  Room: "Аудитория занята",
  StudyGroup: "Группа занята",
};

export const SKIP_REASON_LABEL: Record<GenerationSkipReason, string> = {
  NonWorkingDay: "Нерабочий день",
  Conflict: "Конфликт ресурса",
};

export const DAY_OF_WEEK_LABEL: Record<DayOfWeekName, string> = {
  Monday: "Понедельник",
  Tuesday: "Вторник",
  Wednesday: "Среда",
  Thursday: "Четверг",
  Friday: "Пятница",
  Saturday: "Суббота",
  Sunday: "Воскресенье",
};

export const DAY_OF_WEEK_SHORT: Record<DayOfWeekName, string> = {
  Monday: "Пн",
  Tuesday: "Вт",
  Wednesday: "Ср",
  Thursday: "Чт",
  Friday: "Пт",
  Saturday: "Сб",
  Sunday: "Вс",
};

/** A session is frozen once it is held, cancelled or rescheduled — the card
 *  goes read-only and lifecycle/edit requests are blocked client-side (the
 *  server would 409 anyway). */
export function isTerminalSession(status: SessionStatus): boolean {
  return status !== "Planned";
}

/** `"HH:mm:ss"` (server `TimeOnly`) → `"HH:mm"` for display / <input type=time>. */
export function trimSeconds(time: string): string {
  return time.length >= 5 ? time.slice(0, 5) : time;
}

// Stable-ish palette for colour-by-group. Chroma-0 neutrals are a theme rule
// for the *chrome*; event chips are content and may carry hue. We derive a
// hue from the group id so the same group keeps its colour across renders.
const EVENT_HUES = [8, 45, 130, 190, 260, 310, 95, 220];

/** Pull the human-readable conflict lines out of a 409 from the reschedule /
 *  create-session endpoints. The backend's `CustomException` puts the
 *  per-resource lines in `problem.errors` as a plain string array (not the
 *  FluentValidation `Record<string,string[]>`), with a summary in
 *  `problem.detail`. */
export function conflictLines(err: unknown): string[] {
  if (err instanceof ApiRequestError && err.status === 409) {
    const raw = err.problem?.errors as unknown;
    if (Array.isArray(raw) && raw.length > 0) return raw.map(String);
    if (raw && typeof raw === "object") {
      return Object.values(raw as Record<string, string[]>).flat();
    }
    if (err.problem?.detail) return [err.problem.detail];
  }
  return [describe(err)];
}

export function groupColor(studyGroupId: string): string {
  let hash = 0;
  for (let i = 0; i < studyGroupId.length; i += 1) {
    hash = (hash * 31 + studyGroupId.charCodeAt(i)) >>> 0;
  }
  const hue = EVENT_HUES[hash % EVENT_HUES.length];
  return `oklch(0.62 0.12 ${hue})`;
}
