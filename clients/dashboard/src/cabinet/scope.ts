import { useQuery } from "@tanstack/react-query";
import { getMyPeopleScope } from "@/api/people";

// ─────────────────────────────────────────────────────────────────────────
//  Кабинет — общий запрос «кто я в предметной области».
//
//  GET /api/v1/people/me/scope не требует права (self-lookup, как
//  Multitenancy `GET /tenants/me/status`). Держим единый ключ, чтобы
//  лендинг по роли, переключатель подопечных и «свои» экраны читали один
//  кэш, а не били эндпоинт по разу на страницу.
// ─────────────────────────────────────────────────────────────────────────

export const PEOPLE_SCOPE_KEY = ["people", "me", "scope"] as const;

export function useMyPeopleScope() {
  return useQuery({
    queryKey: PEOPLE_SCOPE_KEY,
    queryFn: getMyPeopleScope,
    // Роль пользователя внутри школы меняется редко; лишние рефетчи не нужны.
    staleTime: 5 * 60_000,
    // Ошибку не ретраим — лендинг деградирует до обзора менеджера сам.
    retry: false,
  });
}
