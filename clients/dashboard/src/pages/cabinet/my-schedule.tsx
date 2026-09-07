import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarPlus, CalendarRange, Copy, RefreshCw, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  getIcalSubscription,
  getMySchedule,
  icalSubscriptionUrl,
  revokeIcalSubscription,
  rotateIcalSubscription,
} from "@/api/scheduling";
import { searchStudyGroups } from "@/api/study-groups";
import { getTenantSettings } from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { WardSwitcher } from "@/cabinet/ward-switcher";
import { useWard } from "@/cabinet/use-ward";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
} from "@/pages/scheduling/scheduling-ui";

const RANGES = [
  { key: "7", label: "7 дней", days: 7 },
  { key: "14", label: "2 недели", days: 14 },
  { key: "30", label: "Месяц", days: 30 },
] as const;

export function CabinetSchedulePage() {
  const perms = useAuth().user?.permissions ?? [];
  const canViewOwn = perms.includes("Permissions.Scheduling.Sessions.ViewOwn");
  const [rangeKey, setRangeKey] = useState<(typeof RANGES)[number]["key"]>("14");
  const { selectedWardId, selectedWard } = useWard();

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
    queryKey: ["sessions", "my", { from, to, ward: selectedWardId }],
    queryFn: () => getMySchedule(from, to, selectedWardId),
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
        <PageHero eyebrow="Кабинет" title="Моё расписание" />
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
        eyebrow="Кабинет"
        title="Моё расписание"
        subtitle={
          selectedWard
            ? `Занятия групп подопечного «${selectedWard.name}». Часовой пояс школы: ${tz}.`
            : `Ближайшие занятия — ваши как преподавателя, ваших групп или групп подопечных. Часовой пояс школы: ${tz}.`
        }
      />

      <WardSwitcher />

      <CalendarSubscription />

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

/**
 * EDX-012 — "Добавить в календарь". Exposes the caller's personal iCal feed URL
 * (`GET /my/schedule.ics?tenant=…&token=…`, anonymous, token-authenticated) so a
 * calendar app can subscribe to it. The link is a personal secret — rotating or
 * revoking invalidates every copy.
 */
function CalendarSubscription() {
  const queryClient = useQueryClient();
  const [revealed, setRevealed] = useState(false);

  const query = useQuery({
    queryKey: ["ical-subscription"],
    queryFn: getIcalSubscription,
    staleTime: 5 * 60_000,
  });

  const rotate = useMutation({
    mutationFn: rotateIcalSubscription,
    onSuccess: (data) => {
      queryClient.setQueryData(["ical-subscription"], data);
      setRevealed(true);
    },
    onError: () => toast.error("Не удалось создать ссылку"),
  });

  const revoke = useMutation({
    mutationFn: revokeIcalSubscription,
    onSuccess: () => {
      queryClient.setQueryData(["ical-subscription"], null);
      setRevealed(false);
    },
    onError: () => toast.error("Не удалось отозвать ссылку"),
  });

  const sub = query.data ?? null;
  const url = sub ? icalSubscriptionUrl(sub.path) : "";

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(url);
      toast.success("Ссылка скопирована");
    } catch {
      setRevealed(true);
      toast.message("Скопируйте ссылку вручную");
    }
  };

  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-[13px] font-semibold text-[var(--color-foreground)]">
            Подписка на календарь
          </h2>
          <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
            Персональная ссылка на ваши занятия в формате iCal. Вставьте её в Google Календарь
            или Apple Календарь («Подписаться по URL»). Ссылка личная — не делитесь ей.
          </p>
        </div>
        {sub === null && (
          <Button
            size="sm"
            onClick={() => rotate.mutate()}
            disabled={rotate.isPending || query.isLoading}
          >
            <CalendarPlus className="mr-1.5 h-3.5 w-3.5" />
            Добавить в календарь
          </Button>
        )}
      </div>

      {sub !== null && (
        <div className="mt-3 space-y-2">
          <div className="flex items-center gap-2">
            <Input
              readOnly
              value={revealed ? url : url.replace(/token=[^&]+/, "token=••••••••")}
              onFocus={(e) => {
                setRevealed(true);
                e.currentTarget.select();
              }}
              className="font-mono text-[11.5px]"
              aria-label="Ссылка на календарь"
            />
            <Button size="sm" variant="outline" onClick={copy} title="Скопировать">
              <Copy className="h-3.5 w-3.5" />
            </Button>
          </div>
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                if (
                  window.confirm(
                    "Создать новую ссылку? Прежняя перестанет работать во всех календарях.",
                  )
                ) {
                  rotate.mutate();
                }
              }}
              disabled={rotate.isPending}
            >
              <RefreshCw className="mr-1.5 h-3.5 w-3.5" />
              Обновить ссылку
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => {
                if (window.confirm("Отозвать ссылку? Календари перестанут получать обновления.")) {
                  revoke.mutate();
                }
              }}
              disabled={revoke.isPending}
            >
              <Trash2 className="mr-1.5 h-3.5 w-3.5" />
              Отозвать
            </Button>
          </div>
        </div>
      )}
    </section>
  );
}
