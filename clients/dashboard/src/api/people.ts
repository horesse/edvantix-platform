import { apiFetch } from "@/lib/api-client";

// ─────────────────────────────────────────────────────────────────────────
//  People API — students, teachers, guardians.
//
//  Hand-written types mirroring `Modules.People.Contracts` (there is no
//  codegen step, see frontend/shared.md). Flat resource routing under
//  `/api/v1` — no `/people` segment — except the scope resolver.
//  Backend reference: docs/02 Модули/People.md → "Контракты".
// ─────────────────────────────────────────────────────────────────────────

export type PagedResponse<T> = {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};

/** Lifecycle: Lead → Active → Paused ↔ Active → Archived ↔ Active. */
export type StudentStatus = "Lead" | "Active" | "Paused" | "Archived";
export type TeacherStatus = "Active" | "Inactive";

// ─── Students ─────────────────────────────────────────────────────────

export type StudentDto = {
  id: string;
  lastName: string;
  firstName: string;
  middleName?: string | null;
  displayName: string;
  /** `yyyy-MM-dd` (server `DateOnly`). */
  birthDate: string;
  phone: string;
  email: string;
  userId?: string | null;
  status: StudentStatus;
  source?: string | null;
  avatarFileId?: string | null;
  managerUserId: string;
  enrolledAtUtc: string;
};

export type StudentDetailDto = StudentDto & {
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  guardianCount: number;
  noteCount: number;
};

export type StudentNoteDto = {
  id: string;
  studentId: string;
  text: string;
  authorUserId: string;
  createdAtUtc: string;
};

export type StudentGuardianDto = {
  id: string;
  studentId: string;
  guardianId: string;
  relation: string;
  isPrimaryPayer: boolean;
  guardian: GuardianDto;
};

export type SearchStudentsParams = {
  search?: string;
  status?: StudentStatus | null;
  managerUserId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateStudentInput = {
  lastName: string;
  firstName: string;
  middleName?: string | null;
  birthDate: string;
  phone: string;
  email: string;
  managerUserId: string;
  source?: string | null;
};

export type UpdateStudentInput = CreateStudentInput & { studentId: string };

const STUDENTS = "/api/v1/students";

export function searchStudents(
  params: SearchStudentsParams = {},
): Promise<PagedResponse<StudentDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.status) q.set("status", params.status);
  if (params.managerUserId) q.set("managerUserId", params.managerUserId);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<StudentDto>>(`${STUDENTS}?${q.toString()}`);
}

export function getStudentById(id: string): Promise<StudentDetailDto> {
  return apiFetch<StudentDetailDto>(`${STUDENTS}/${encodeURIComponent(id)}`);
}

export function createStudent(input: CreateStudentInput): Promise<string> {
  return apiFetch<string>(STUDENTS, {
    method: "POST",
    body: JSON.stringify({
      lastName: input.lastName,
      firstName: input.firstName,
      middleName: input.middleName ?? null,
      birthDate: input.birthDate,
      phone: input.phone,
      email: input.email,
      managerUserId: input.managerUserId,
      source: input.source ?? null,
    }),
  });
}

export function updateStudent(input: UpdateStudentInput): Promise<string> {
  return apiFetch<string>(`${STUDENTS}/${encodeURIComponent(input.studentId)}`, {
    method: "PUT",
    body: JSON.stringify({
      studentId: input.studentId,
      lastName: input.lastName,
      firstName: input.firstName,
      middleName: input.middleName ?? null,
      birthDate: input.birthDate,
      phone: input.phone,
      email: input.email,
      managerUserId: input.managerUserId,
      source: input.source ?? null,
    }),
  });
}

export async function deleteStudent(id: string): Promise<void> {
  await apiFetch<void>(`${STUDENTS}/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export async function archiveStudent(id: string): Promise<void> {
  await apiFetch<void>(`${STUDENTS}/${encodeURIComponent(id)}/archive`, { method: "POST" });
}

export async function restoreStudent(id: string): Promise<void> {
  await apiFetch<void>(`${STUDENTS}/${encodeURIComponent(id)}/restore`, { method: "POST" });
}

export async function linkStudentUser(id: string, userId: string): Promise<void> {
  await apiFetch<void>(`${STUDENTS}/${encodeURIComponent(id)}/link-user`, {
    method: "POST",
    body: JSON.stringify({ userId }),
  });
}

export async function unlinkStudentUser(id: string): Promise<void> {
  await apiFetch<void>(`${STUDENTS}/${encodeURIComponent(id)}/unlink-user`, { method: "POST" });
}

// ── Student ↔ guardians ──

export function getStudentGuardians(studentId: string): Promise<StudentGuardianDto[]> {
  return apiFetch<StudentGuardianDto[]>(
    `${STUDENTS}/${encodeURIComponent(studentId)}/guardians`,
  );
}

export type AddStudentGuardianInput = {
  studentId: string;
  guardianId: string;
  relation: string;
  isPrimaryPayer?: boolean;
};

export function addStudentGuardian(input: AddStudentGuardianInput): Promise<string> {
  return apiFetch<string>(
    `${STUDENTS}/${encodeURIComponent(input.studentId)}/guardians`,
    {
      method: "POST",
      body: JSON.stringify({
        guardianId: input.guardianId,
        relation: input.relation,
        isPrimaryPayer: input.isPrimaryPayer ?? false,
      }),
    },
  );
}

export async function removeStudentGuardian(
  studentId: string,
  guardianId: string,
): Promise<void> {
  await apiFetch<void>(
    `${STUDENTS}/${encodeURIComponent(studentId)}/guardians/${encodeURIComponent(guardianId)}`,
    { method: "DELETE" },
  );
}

export async function setPrimaryPayer(studentId: string, guardianId: string): Promise<void> {
  await apiFetch<void>(
    `${STUDENTS}/${encodeURIComponent(studentId)}/guardians/${encodeURIComponent(guardianId)}/primary-payer`,
    { method: "POST" },
  );
}

// ── Student notes (gated by Students.ViewNotes) ──

export function getStudentNotes(studentId: string): Promise<StudentNoteDto[]> {
  return apiFetch<StudentNoteDto[]>(
    `${STUDENTS}/${encodeURIComponent(studentId)}/notes`,
  );
}

export function addStudentNote(studentId: string, text: string): Promise<string> {
  return apiFetch<string>(`${STUDENTS}/${encodeURIComponent(studentId)}/notes`, {
    method: "POST",
    body: JSON.stringify({ text }),
  });
}

export async function deleteStudentNote(studentId: string, noteId: string): Promise<void> {
  await apiFetch<void>(
    `${STUDENTS}/${encodeURIComponent(studentId)}/notes/${encodeURIComponent(noteId)}`,
    { method: "DELETE" },
  );
}

// ── CSV import ──

export type ImportStudentRowResultDto = {
  rowNumber: number;
  success: boolean;
  studentId?: string | null;
  error?: string | null;
};

export type ImportStudentsResultDto = {
  dryRun: boolean;
  totalRows: number;
  successCount: number;
  errorCount: number;
  rows: ImportStudentRowResultDto[];
};

/**
 * Uploads a CSV of students. `dryRun: true` (default) validates every row and
 * reports what would happen without writing; failing rows never block the rest.
 * Re-send with `dryRun: false` to commit.
 */
export function importStudents(
  file: File,
  dryRun: boolean,
): Promise<ImportStudentsResultDto> {
  const form = new FormData();
  form.append("file", file);
  return apiFetch<ImportStudentsResultDto>(
    `${STUDENTS}/import?dryRun=${dryRun ? "true" : "false"}`,
    { method: "POST", body: form, timeoutMs: 120_000 },
  );
}

// ─── Teachers ─────────────────────────────────────────────────────────

export type TeacherDto = {
  id: string;
  lastName: string;
  firstName: string;
  middleName?: string | null;
  displayName: string;
  phone: string;
  email: string;
  userId?: string | null;
  status: TeacherStatus;
  bio?: string | null;
  specializations: string[];
  hourlyRate?: number | null;
  avatarFileId?: string | null;
};

export type SearchTeachersParams = {
  search?: string;
  status?: TeacherStatus | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateTeacherInput = {
  lastName: string;
  firstName: string;
  middleName?: string | null;
  phone: string;
  email: string;
  bio?: string | null;
  specializations?: string[] | null;
  hourlyRate?: number | null;
};

export type UpdateTeacherInput = CreateTeacherInput & { teacherId: string };

const TEACHERS = "/api/v1/teachers";

export function searchTeachers(
  params: SearchTeachersParams = {},
): Promise<PagedResponse<TeacherDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  if (params.status) q.set("status", params.status);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<TeacherDto>>(`${TEACHERS}?${q.toString()}`);
}

export function getTeacherById(id: string): Promise<TeacherDto> {
  return apiFetch<TeacherDto>(`${TEACHERS}/${encodeURIComponent(id)}`);
}

export function createTeacher(input: CreateTeacherInput): Promise<string> {
  return apiFetch<string>(TEACHERS, {
    method: "POST",
    body: JSON.stringify({
      lastName: input.lastName,
      firstName: input.firstName,
      middleName: input.middleName ?? null,
      phone: input.phone,
      email: input.email,
      bio: input.bio ?? null,
      specializations: input.specializations ?? null,
      hourlyRate: input.hourlyRate ?? null,
    }),
  });
}

export function updateTeacher(input: UpdateTeacherInput): Promise<string> {
  return apiFetch<string>(`${TEACHERS}/${encodeURIComponent(input.teacherId)}`, {
    method: "PUT",
    body: JSON.stringify({
      teacherId: input.teacherId,
      lastName: input.lastName,
      firstName: input.firstName,
      middleName: input.middleName ?? null,
      phone: input.phone,
      email: input.email,
      bio: input.bio ?? null,
      specializations: input.specializations ?? null,
      hourlyRate: input.hourlyRate ?? null,
    }),
  });
}

export async function deleteTeacher(id: string): Promise<void> {
  await apiFetch<void>(`${TEACHERS}/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export async function deactivateTeacher(id: string): Promise<void> {
  await apiFetch<void>(`${TEACHERS}/${encodeURIComponent(id)}/deactivate`, { method: "POST" });
}

export async function activateTeacher(id: string): Promise<void> {
  await apiFetch<void>(`${TEACHERS}/${encodeURIComponent(id)}/activate`, { method: "POST" });
}

export async function linkTeacherUser(id: string, userId: string): Promise<void> {
  await apiFetch<void>(`${TEACHERS}/${encodeURIComponent(id)}/link-user`, {
    method: "POST",
    body: JSON.stringify({ userId }),
  });
}

export async function unlinkTeacherUser(id: string): Promise<void> {
  await apiFetch<void>(`${TEACHERS}/${encodeURIComponent(id)}/unlink-user`, { method: "POST" });
}

// ─── Guardians ────────────────────────────────────────────────────────

export type GuardianDto = {
  id: string;
  lastName: string;
  firstName: string;
  displayName: string;
  phone: string;
  email: string;
  userId?: string | null;
};

export type SearchGuardiansParams = {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateGuardianInput = {
  lastName: string;
  firstName: string;
  phone: string;
  email: string;
};

export type UpdateGuardianInput = CreateGuardianInput & { guardianId: string };

const GUARDIANS = "/api/v1/guardians";

export function searchGuardians(
  params: SearchGuardiansParams = {},
): Promise<PagedResponse<GuardianDto>> {
  const q = new URLSearchParams();
  if (params.search) q.set("search", params.search);
  q.set("pageNumber", String(params.pageNumber ?? 1));
  q.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) q.set("sortBy", params.sortBy);
  if (params.sortDir) q.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<GuardianDto>>(`${GUARDIANS}?${q.toString()}`);
}

export function getGuardianById(id: string): Promise<GuardianDto> {
  return apiFetch<GuardianDto>(`${GUARDIANS}/${encodeURIComponent(id)}`);
}

export function createGuardian(input: CreateGuardianInput): Promise<string> {
  return apiFetch<string>(GUARDIANS, {
    method: "POST",
    body: JSON.stringify({
      lastName: input.lastName,
      firstName: input.firstName,
      phone: input.phone,
      email: input.email,
    }),
  });
}

export function updateGuardian(input: UpdateGuardianInput): Promise<string> {
  return apiFetch<string>(`${GUARDIANS}/${encodeURIComponent(input.guardianId)}`, {
    method: "PUT",
    body: JSON.stringify({
      guardianId: input.guardianId,
      lastName: input.lastName,
      firstName: input.firstName,
      phone: input.phone,
      email: input.email,
    }),
  });
}

export async function deleteGuardian(id: string): Promise<void> {
  await apiFetch<void>(`${GUARDIANS}/${encodeURIComponent(id)}`, { method: "DELETE" });
}

export async function linkGuardianUser(id: string, userId: string): Promise<void> {
  await apiFetch<void>(`${GUARDIANS}/${encodeURIComponent(id)}/link-user`, {
    method: "POST",
    body: JSON.stringify({ userId }),
  });
}

export async function unlinkGuardianUser(id: string): Promise<void> {
  await apiFetch<void>(`${GUARDIANS}/${encodeURIComponent(id)}/unlink-user`, { method: "POST" });
}

// ─── People scope (current user) ──────────────────────────────────────

export type PeopleScope = {
  studentId?: string | null;
  teacherId?: string | null;
  guardianId?: string | null;
  wardStudentIds: string[];
};

export function getMyPeopleScope(): Promise<PeopleScope> {
  return apiFetch<PeopleScope>("/api/v1/people/me/scope");
}
