import type { ComboboxOption, EntityStatusTone } from "@/components/list";
import type { CourseLevel, CourseStatus, SubjectNodeDto } from "@/api/curriculum";

// Shared display maps + helpers for the Curriculum screens. Kept in a
// component-free module so importing them doesn't trip react-refresh.

export const LEVEL_LABEL: Record<CourseLevel, string> = {
  Beginner: "Начальный",
  Elementary: "Базовый",
  Intermediate: "Средний",
  Advanced: "Продвинутый",
};

export const STATUS_LABEL: Record<CourseStatus, string> = {
  Draft: "Черновик",
  Published: "Опубликован",
  Archived: "Архив",
};

export const STATUS_TONE: Record<CourseStatus, EntityStatusTone> = {
  Draft: "default",
  Published: "success",
  Archived: "warning",
};

/** Flatten the subject tree into indented combobox options (leading spaces
 *  give a cheap depth cue without a custom renderer). */
export function flattenSubjects(
  nodes: SubjectNodeDto[],
  depth = 0,
): ComboboxOption[] {
  return nodes.flatMap((n) => [
    { value: n.id, label: `${"  ".repeat(depth)}${n.name}` },
    ...flattenSubjects(n.children, depth + 1),
  ]);
}
