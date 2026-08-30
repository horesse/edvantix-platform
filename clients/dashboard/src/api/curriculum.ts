import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/people";

// ─────────────────────────────────────────────────────────────────────────
//  Curriculum API — subjects, courses, modules, lessons, lesson materials.
//
//  Hand-written types mirroring `Modules.Curriculum.Contracts` (there is no
//  codegen step, see frontend/shared.md). Flat resource routing under
//  `/api/v1` — no `/curriculum` segment.
//  Backend reference: docs/02 Модули/Curriculum.md → "Контракты".
// ─────────────────────────────────────────────────────────────────────────

export type { PagedResponse };

/** `Beginner → Elementary → Intermediate → Advanced`. */
export type CourseLevel = "Beginner" | "Elementary" | "Intermediate" | "Advanced";
/** Lifecycle: `Draft → Published ↔ Archived`. */
export type CourseStatus = "Draft" | "Published" | "Archived";
export type MaterialKind = "File" | "Video" | "Link" | "Homework" | "Presentation";

export const COURSE_LEVELS: CourseLevel[] = [
  "Beginner",
  "Elementary",
  "Intermediate",
  "Advanced",
];
export const COURSE_STATUSES: CourseStatus[] = ["Draft", "Published", "Archived"];
export const MATERIAL_KINDS: MaterialKind[] = [
  "File",
  "Video",
  "Link",
  "Homework",
  "Presentation",
];

// ─── Subjects ─────────────────────────────────────────────────────────

export type SubjectDto = {
  id: string;
  parentId?: string | null;
  name: string;
  slug: string;
  sortOrder: number;
};

export type SubjectNodeDto = {
  id: string;
  name: string;
  slug: string;
  sortOrder: number;
  children: SubjectNodeDto[];
};

const SUBJECTS = "/api/v1/subjects";

export function getSubjectTree(): Promise<SubjectNodeDto[]> {
  return apiFetch<SubjectNodeDto[]>(`${SUBJECTS}/tree`);
}

export function createSubject(input: {
  name: string;
  parentId?: string | null;
}): Promise<string> {
  return apiFetch<string>(SUBJECTS, {
    method: "POST",
    body: JSON.stringify({ name: input.name, parentId: input.parentId ?? null }),
  });
}

export async function updateSubject(input: {
  subjectId: string;
  name: string;
  parentId?: string | null;
}): Promise<void> {
  await apiFetch<void>(`${SUBJECTS}/${encodeURIComponent(input.subjectId)}`, {
    method: "PUT",
    body: JSON.stringify({ name: input.name, parentId: input.parentId ?? null }),
  });
}

export async function deleteSubject(subjectId: string): Promise<void> {
  await apiFetch<void>(`${SUBJECTS}/${encodeURIComponent(subjectId)}`, {
    method: "DELETE",
  });
}

/** Sets `SortOrder = 0, 1, 2…` for the siblings under `parentId` (null = top
 *  level) in the order supplied. Only one level at a time — drag-n-drop stays
 *  within a parent. */
export async function reorderSubjects(input: {
  parentId: string | null;
  orderedSubjectIds: string[];
}): Promise<void> {
  await apiFetch<void>(`${SUBJECTS}/order`, {
    method: "PUT",
    body: JSON.stringify({
      parentId: input.parentId,
      orderedSubjectIds: input.orderedSubjectIds,
    }),
  });
}

// ─── Courses ──────────────────────────────────────────────────────────

export type CourseDto = {
  id: string;
  subjectId: string;
  title: string;
  slug: string;
  description?: string | null;
  level: CourseLevel;
  durationHours: number;
  status: CourseStatus;
  coverFileId?: string | null;
  publishedAtUtc?: string | null;
  createdAtUtc: string;
};

export type LessonDto = {
  id: string;
  courseModuleId: string;
  title: string;
  objectives?: string | null;
  content?: string | null;
  durationMinutes: number;
  sortOrder: number;
};

export type CourseModuleWithLessonsDto = {
  id: string;
  title: string;
  description?: string | null;
  sortOrder: number;
  lessons: LessonDto[];
};

export type CourseDetailDto = {
  id: string;
  subjectId: string;
  title: string;
  slug: string;
  description?: string | null;
  level: CourseLevel;
  durationHours: number;
  status: CourseStatus;
  coverFileId?: string | null;
  publishedAtUtc?: string | null;
  createdAtUtc: string;
  modules: CourseModuleWithLessonsDto[];
};

/** Standalone module projection (not currently returned by any endpoint the
 *  dashboard calls — the builder reads modules off `CourseDetailDto`). */
export type CourseModuleDto = {
  id: string;
  courseId: string;
  title: string;
  description?: string | null;
  sortOrder: number;
};

export type SearchCoursesParams = {
  search?: string;
  subjectId?: string | null;
  status?: CourseStatus | null;
  level?: CourseLevel | null;
  pageNumber?: number;
  pageSize?: number;
  /** One of `title | createdAtUtc | durationHours`. */
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

const COURSES = "/api/v1/courses";

export function searchCourses(
  params: SearchCoursesParams = {},
): Promise<PagedResponse<CourseDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.subjectId) q.set("subjectId", params.subjectId);
  if (params.status) q.set("status", params.status);
  if (params.level) q.set("level", params.level);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<CourseDto>>(`${COURSES}?${q.toString()}`);
}

export function listTrashedCourses(
  pageNumber = 1,
  pageSize = 20,
): Promise<PagedResponse<CourseDto>> {
  return apiFetch<PagedResponse<CourseDto>>(
    `${COURSES}/trash?pageNumber=${pageNumber}&pageSize=${pageSize}`,
  );
}

export function getCourseById(id: string): Promise<CourseDetailDto> {
  return apiFetch<CourseDetailDto>(`${COURSES}/${encodeURIComponent(id)}`);
}

export type CreateCourseInput = {
  subjectId: string;
  title: string;
  description?: string | null;
  level: CourseLevel;
  durationHours: number;
  coverFileId?: string | null;
};

export function createCourse(input: CreateCourseInput): Promise<string> {
  return apiFetch<string>(COURSES, {
    method: "POST",
    body: JSON.stringify({
      subjectId: input.subjectId,
      title: input.title,
      description: input.description ?? null,
      level: input.level,
      durationHours: input.durationHours,
      coverFileId: input.coverFileId ?? null,
    }),
  });
}

export type UpdateCourseInput = CreateCourseInput & { courseId: string };

export async function updateCourse(input: UpdateCourseInput): Promise<void> {
  await apiFetch<void>(`${COURSES}/${encodeURIComponent(input.courseId)}`, {
    method: "PUT",
    body: JSON.stringify({
      subjectId: input.subjectId,
      title: input.title,
      description: input.description ?? null,
      level: input.level,
      durationHours: input.durationHours,
      coverFileId: input.coverFileId ?? null,
    }),
  });
}

export async function deleteCourse(id: string): Promise<void> {
  await apiFetch<void>(`${COURSES}/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export async function publishCourse(id: string): Promise<void> {
  await apiFetch<void>(`${COURSES}/${encodeURIComponent(id)}/publish`, {
    method: "POST",
  });
}

export async function archiveCourse(id: string): Promise<void> {
  await apiFetch<void>(`${COURSES}/${encodeURIComponent(id)}/archive`, {
    method: "POST",
  });
}

export function duplicateCourse(id: string): Promise<string> {
  return apiFetch<string>(`${COURSES}/${encodeURIComponent(id)}/duplicate`, {
    method: "POST",
  });
}

export function restoreCourse(id: string): Promise<string> {
  return apiFetch<string>(`${COURSES}/${encodeURIComponent(id)}/restore`, {
    method: "POST",
  });
}

// ─── Course modules (sections) ────────────────────────────────────────

export function createCourseModule(input: {
  courseId: string;
  title: string;
  description?: string | null;
}): Promise<string> {
  return apiFetch<string>(
    `${COURSES}/${encodeURIComponent(input.courseId)}/modules`,
    {
      method: "POST",
      body: JSON.stringify({
        title: input.title,
        description: input.description ?? null,
      }),
    },
  );
}

export async function updateCourseModule(input: {
  moduleId: string;
  title: string;
  description?: string | null;
}): Promise<void> {
  await apiFetch<void>(`/api/v1/modules/${encodeURIComponent(input.moduleId)}`, {
    method: "PUT",
    body: JSON.stringify({
      title: input.title,
      description: input.description ?? null,
    }),
  });
}

export async function deleteCourseModule(moduleId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/modules/${encodeURIComponent(moduleId)}`, {
    method: "DELETE",
  });
}

export async function reorderCourseModules(input: {
  courseId: string;
  orderedModuleIds: string[];
}): Promise<void> {
  await apiFetch<void>(
    `${COURSES}/${encodeURIComponent(input.courseId)}/modules/reorder`,
    {
      method: "PUT",
      body: JSON.stringify({ orderedModuleIds: input.orderedModuleIds }),
    },
  );
}

// ─── Lessons ──────────────────────────────────────────────────────────

export function createLesson(input: {
  moduleId: string;
  title: string;
  objectives?: string | null;
  content?: string | null;
  durationMinutes: number;
}): Promise<string> {
  return apiFetch<string>(
    `/api/v1/modules/${encodeURIComponent(input.moduleId)}/lessons`,
    {
      method: "POST",
      body: JSON.stringify({
        title: input.title,
        objectives: input.objectives ?? null,
        content: input.content ?? null,
        durationMinutes: input.durationMinutes,
      }),
    },
  );
}

export type UpdateLessonInput = {
  lessonId: string;
  title: string;
  objectives?: string | null;
  content?: string | null;
  durationMinutes: number;
};

export async function updateLesson(input: UpdateLessonInput): Promise<void> {
  await apiFetch<void>(`/api/v1/lessons/${encodeURIComponent(input.lessonId)}`, {
    method: "PUT",
    body: JSON.stringify({
      title: input.title,
      objectives: input.objectives ?? null,
      content: input.content ?? null,
      durationMinutes: input.durationMinutes,
    }),
  });
}

export async function deleteLesson(lessonId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/lessons/${encodeURIComponent(lessonId)}`, {
    method: "DELETE",
  });
}

export async function reorderLessons(input: {
  moduleId: string;
  orderedLessonIds: string[];
}): Promise<void> {
  await apiFetch<void>(
    `/api/v1/modules/${encodeURIComponent(input.moduleId)}/lessons/reorder`,
    {
      method: "PUT",
      body: JSON.stringify({ orderedLessonIds: input.orderedLessonIds }),
    },
  );
}

// ─── Lesson materials ─────────────────────────────────────────────────

export type LessonMaterialDto = {
  id: string;
  lessonId: string;
  kind: MaterialKind;
  title: string;
  fileId?: string | null;
  url?: string | null;
  visibleToStudents: boolean;
  sortOrder: number;
};

export function getLessonMaterials(lessonId: string): Promise<LessonMaterialDto[]> {
  return apiFetch<LessonMaterialDto[]>(
    `/api/v1/lessons/${encodeURIComponent(lessonId)}/materials`,
  );
}

export type AddLessonMaterialInput = {
  lessonId: string;
  kind: MaterialKind;
  title: string;
  /** Exactly one of `fileId` / `url` — validated client-side, enforced server-side. */
  fileId?: string | null;
  url?: string | null;
  visibleToStudents: boolean;
};

export function addLessonMaterial(
  input: AddLessonMaterialInput,
): Promise<LessonMaterialDto> {
  return apiFetch<LessonMaterialDto>(
    `/api/v1/lessons/${encodeURIComponent(input.lessonId)}/materials`,
    {
      method: "POST",
      body: JSON.stringify({
        kind: input.kind,
        title: input.title,
        fileId: input.fileId ?? null,
        url: input.url ?? null,
        visibleToStudents: input.visibleToStudents,
      }),
    },
  );
}

export async function removeLessonMaterial(materialId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/materials/${encodeURIComponent(materialId)}`, {
    method: "DELETE",
  });
}

export async function reorderLessonMaterials(input: {
  lessonId: string;
  orderedMaterialIds: string[];
}): Promise<void> {
  await apiFetch<void>(
    `/api/v1/lessons/${encodeURIComponent(input.lessonId)}/materials/reorder`,
    {
      method: "PUT",
      body: JSON.stringify({ orderedMaterialIds: input.orderedMaterialIds }),
    },
  );
}
