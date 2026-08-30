import type { EntityStatusTone } from "@/components/list";
import type {
  EnrollmentStatus,
  GroupFormat,
  StudyGroupStatus,
  TeacherRole,
} from "@/api/study-groups";

// Shared display maps for the StudyGroups screens. Component-free module so
// importing them doesn't trip react-refresh (same pattern as curriculum-ui.ts).

export const FORMAT_LABEL: Record<GroupFormat, string> = {
  Online: "Онлайн",
  Offline: "Очно",
  Hybrid: "Гибрид",
};

export const STATUS_LABEL: Record<StudyGroupStatus, string> = {
  Forming: "Набор",
  Active: "Идёт",
  Finished: "Завершена",
  Cancelled: "Отменена",
};

export const STATUS_TONE: Record<StudyGroupStatus, EntityStatusTone> = {
  Forming: "info",
  Active: "success",
  Finished: "default",
  Cancelled: "danger",
};

export const ENROLLMENT_STATUS_LABEL: Record<EnrollmentStatus, string> = {
  Active: "Активен",
  Paused: "Пауза",
  Left: "Ушёл",
  Completed: "Завершил",
};

export const ENROLLMENT_STATUS_TONE: Record<EnrollmentStatus, EntityStatusTone> = {
  Active: "success",
  Paused: "warning",
  Left: "default",
  Completed: "info",
};

export const TEACHER_ROLE_LABEL: Record<TeacherRole, string> = {
  Primary: "Основной",
  Assistant: "Ассистент",
  Substitute: "Замена",
};

/** A group is frozen once it finishes or is cancelled — the card and roster
 *  go read-only and lifecycle/roster requests are blocked client-side. */
export function isFrozen(status: StudyGroupStatus): boolean {
  return status === "Finished" || status === "Cancelled";
}
