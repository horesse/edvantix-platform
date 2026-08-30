import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { CalendarRange } from "lucide-react";
import { getMySchedule } from "@/api/scheduling";
import { searchStudyGroups } from "@/api/study-groups";
import { getTenantSettings } from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  EntityEmpty,
  EntityInitialsAvatar,
  EntityStatusBadge,
  ErrorBand,
  PageHero,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { formatZonedDateTime, formatZonedTime } from "@/lib/tz";
import {
  SESSION_STATUS_LABEL,
  SESSION_STATUS_TONE,
} from "./scheduling-ui";

const RANGES = [
  { key: "7", label: "7 дней", days: 7 },
  { key: "14", label: "2 недели", days: 14 },
  { key: "30", label: "Месяц", days: 30 },
] as const;

export function MySchedulePage() {
  const perms = useAuth().user?.permissions ?? [];
  const canViewOwn = perms.includes("Permissions.Scheduling.Sessions.ViewOwn");
  const [rangeKey, setRangeKey] = useState<(typeof RANGES)[number]["key"]>("14");

  const days = RANGES.find((r) => r.key === rangeKey)!.days;
  const { from, to } = useMemo(() => {
    const now = new Date();
    const start = new Date(now);
    start.setHours(0, 0, 0, 0);
    const end = new Date(start.getTime() + days * 24 * 60 * 60 * 1000);
    return { from: start.toISOString(), to: end.toISOString() };
  }, [days]);

  const settingsQuery = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    staleTime: 5 * 60_000,
  });
  const tz = settingsQuery.data?.timeZoneId || "UTC";

  const query = useQuery({
    queryKey: ["sessions", "my", { from, to }],
    queryFn: () => getMySchedule(from, to),
    enabled: canViewOwn,
  });

  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "my-schedule" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
  });
  const groupName = useMemo(() => {
    const m = new Map<string, string>();
    for (const g of groupsQuery.data?.items ?? []) m.set(g.id, `${g.code} — ${g.name}`);
    return m;
  }, [groupsQuery.data]);

  const items = useMemo(
    () => [...(query.data ?? [])].sort((a, b) => a.startUtc.localeCompare(b.startUtc)),
    [query.data],
  );

  if (!canViewOwn) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Расписание" title="Моё расписание" />
        <EntityEmpty
          icon={CalendarRange}
          title="Нет доступа"
          body="Нужно право «Просмотр своих занятий»."
        />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Расписание"
        title="Моё расписание"
        subtitle={`Ближайшие занятия — ваши как преподавателя или занятия ваших групп. Часовой пояс школы: ${tz}.`}
      />

      <div className="flex items-center gap-1">
        {RANGES.map((r) => (
          <Button
            key={r.key}
            variant={rangeKey === r.key ? "default" : "outline"}
            size="sm"
            onClick={() => setRangeKey(r.key)}
          >
            {r.label}
          </Button>
        ))}
      </div>

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={CalendarRange}
          title="Занятий нет"
          body="В выбранном промежутке для вас нет запланированных занятий."
        />
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)] rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
          {items.map((s) => (
            <li key={s.id} className="px-4 py-3 first:rounded-t-xl last:rounded-b-xl">
              <Link
                to={`/sessions/${s.id}`}
                className="flex items-center justify-between gap-3"
              >
                <div className="flex min-w-0 items-center gap-3">
                  <EntityInitialsAvatar
                    name={groupName.get(s.studyGroupId) ?? "Занятие"}
                    size={36}
                  />
                  <div className="min-w-0">
                    <p className="truncate text-[13px] font-medium text-[var(--color-foreground)]">
                      {s.topic || groupName.get(s.studyGroupId) || "Занятие"}
                    </p>
                    <p className="truncate text-[11.5px] text-[var(--color-muted-foreground)]">
                      {formatZonedDateTime(s.startUtc, tz)} –{" "}
                      {formatZonedTime(s.endUtc, tz)}
                    </p>
                  </div>
                </div>
                <EntityStatusBadge tone={SESSION_STATUS_TONE[s.status]}>
                  {SESSION_STATUS_LABEL[s.status]}
                </EntityStatusBadge>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
