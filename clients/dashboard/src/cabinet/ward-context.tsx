import {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { useQueries } from "@tanstack/react-query";
import { getStudentById } from "@/api/people";
import { useAuth } from "@/auth/use-auth";
import { useMyPeopleScope } from "./scope";

// ─────────────────────────────────────────────────────────────────────────
//  Переключатель подопечных представителя.
//
//  `PeopleScope.wardStudentIds` — ученики, привязанные к текущему
//  представителю. Выбранный подопечный — общий контекст кабинета: он влияет
//  на `/my/schedule` (запрос `/sessions/my` уходит с `studentId=<ward>`) и на
//  любые «за подопечного» вьюхи. Для ученика без подопечных переключатель
//  не показывается.
//
//  ФИО подопечного тянем через `GET /students/{id}` (`retry: false`) — если
//  у представителя нет права `People.Students.View`, запрос вернёт 403 и мы
//  деградируем до подписи «Подопечный N», не роняя переключатель.
//
//  Выбор запоминается в `localStorage` (ключ на тенант), чтобы не слетал
//  при навигации между экранами кабинета.
// ─────────────────────────────────────────────────────────────────────────

export type Ward = {
  id: string;
  /** ФИО или запасная подпись «Подопечный N», если ФИО недоступно. */
  name: string;
  /** `true`, если ФИО реально загружено (а не запасная подпись). */
  resolved: boolean;
};

export type WardContextValue = {
  wards: Ward[];
  hasWards: boolean;
  /** `null` — «все подопечные». */
  selectedWardId: string | null;
  setSelectedWardId: (id: string | null) => void;
  /** Выбранный подопечный целиком (или `null` для «всех»). */
  selectedWard: Ward | null;
};

const EMPTY: WardContextValue = {
  wards: [],
  hasWards: false,
  selectedWardId: null,
  setSelectedWardId: () => {},
  selectedWard: null,
};

export const WardContext = createContext<WardContextValue>(EMPTY);

const STORAGE_PREFIX = "fsh.dashboard.cabinet.ward";

function storageKey(tenant: string | undefined): string {
  return `${STORAGE_PREFIX}:${tenant ?? "_default"}`;
}

function readStored(tenant: string | undefined): string | null {
  if (typeof window === "undefined") return null;
  try {
    return window.localStorage.getItem(storageKey(tenant));
  } catch {
    return null;
  }
}

function writeStored(tenant: string | undefined, value: string | null): void {
  if (typeof window === "undefined") return;
  try {
    if (value === null) window.localStorage.removeItem(storageKey(tenant));
    else window.localStorage.setItem(storageKey(tenant), value);
  } catch {
    /* storage unavailable — не критично, выбор просто не переживёт перезагрузку */
  }
}

export function WardProvider({ children }: { children: ReactNode }) {
  const tenant = useAuth().user?.tenant;
  const scopeQuery = useMyPeopleScope();
  const wardIds = useMemo(
    () => scopeQuery.data?.wardStudentIds ?? [],
    [scopeQuery.data],
  );
  const hasWards = wardIds.length > 0;

  // ФИО подопечных — по запросу на каждого; тихо падаем на запасную подпись.
  const briefQueries = useQueries({
    queries: wardIds.map((id) => ({
      queryKey: ["student-brief", id] as const,
      queryFn: () => getStudentById(id),
      enabled: hasWards,
      retry: false,
      staleTime: 5 * 60_000,
    })),
  });

  const wards = useMemo<Ward[]>(
    () =>
      wardIds.map((id, i) => {
        const name = briefQueries[i]?.data?.displayName;
        return {
          id,
          name: name && name.trim() ? name : `Подопечный ${i + 1}`,
          resolved: Boolean(name && name.trim()),
        };
      }),
    [wardIds, briefQueries],
  );

  const [selectedWardId, setSelectedWardIdState] = useState<string | null>(() =>
    readStored(tenant),
  );

  // Смена тенанта (имперсонация) — перечитать выбор из его ключа.
  useEffect(() => {
    setSelectedWardIdState(readStored(tenant));
  }, [tenant]);

  // Выбранный подопечный больше не в списке (отвязали) → сбросить на «всех».
  useEffect(() => {
    if (selectedWardId && wardIds.length > 0 && !wardIds.includes(selectedWardId)) {
      setSelectedWardIdState(null);
      writeStored(tenant, null);
    }
  }, [selectedWardId, wardIds, tenant]);

  const setSelectedWardId = useCallback(
    (id: string | null) => {
      setSelectedWardIdState(id);
      writeStored(tenant, id);
    },
    [tenant],
  );

  const value = useMemo<WardContextValue>(
    () => ({
      wards,
      hasWards,
      selectedWardId: hasWards ? selectedWardId : null,
      setSelectedWardId,
      selectedWard:
        (hasWards && selectedWardId
          ? wards.find((w) => w.id === selectedWardId)
          : null) ?? null,
    }),
    [wards, hasWards, selectedWardId, setSelectedWardId],
  );

  return <WardContext.Provider value={value}>{children}</WardContext.Provider>;
}
