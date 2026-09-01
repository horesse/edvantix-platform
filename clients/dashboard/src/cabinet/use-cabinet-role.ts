import type { PeopleScope } from "@/api/people";
import { useAuth } from "@/auth/use-auth";
import { useMyPeopleScope } from "./scope";

// ─────────────────────────────────────────────────────────────────────────
//  Определение «кто пользователь» для стартовой страницы и меню кабинета.
//
//  Сигнал роли — это НЕ роль из JWT (её в токене дашборда нет, только имена
//  ролей на бэкенде): комбинация прав из `useAuth().user.permissions` и
//  `PeopleScope` (GET /people/me/scope).
//
//   • есть «менеджерское» право (полный список/справочник) → `manager`
//     → существующий обзор школы на `/`;
//   • иначе `scope.teacherId` → `teacher` → его расписание и группы;
//   • иначе `scope.guardianId` / непустой `wardStudentIds` → `guardian`
//     → счета и расписание подопечных;
//   • иначе `scope.studentId` → `student` → свои счета и расписание;
//   • ничего из этого → `unknown` → безопасно показываем обзор.
//
//  Приоритет manager > teacher > guardian > student: один человек может
//  держать несколько ролей одной учёткой (преподаватель, чей ребёнок
//  учится в той же школе), стартовая страница — одна.
// ─────────────────────────────────────────────────────────────────────────

export type CabinetRole = "manager" | "teacher" | "student" | "guardian" | "unknown";

/** Права, любое из которых означает «сотрудник школы, а не ученик/родитель».
 *  Каждое — право основного списочного эндпоинта соответствующего раздела. */
const MANAGER_SIGNALS = [
  "Permissions.People.Students.View",
  "Permissions.People.Teachers.View",
  "Permissions.Payments.StudentInvoices.View",
  "Permissions.Scheduling.Sessions.View",
  "Permissions.Billing.View",
] as const;

export type CabinetRoleState = {
  role: CabinetRole;
  scope: PeopleScope | undefined;
  isLoading: boolean;
  isError: boolean;
};

export function useCabinetRole(): CabinetRoleState {
  const perms = useAuth().user?.permissions ?? [];
  const scopeQuery = useMyPeopleScope();
  const scope = scopeQuery.data;

  const isManager = MANAGER_SIGNALS.some((p) => perms.includes(p));

  let role: CabinetRole = "unknown";
  if (isManager) {
    role = "manager";
  } else if (scope?.teacherId) {
    role = "teacher";
  } else if (scope?.guardianId || (scope?.wardStudentIds?.length ?? 0) > 0) {
    role = "guardian";
  } else if (scope?.studentId) {
    role = "student";
  }

  return {
    role,
    scope,
    isLoading: scopeQuery.isLoading,
    isError: scopeQuery.isError,
  };
}

/** `true` для ролей, которым положен кабинет `/my/*`, а не обзор школы. */
export function isCabinetRole(role: CabinetRole): boolean {
  return role === "teacher" || role === "student" || role === "guardian";
}
