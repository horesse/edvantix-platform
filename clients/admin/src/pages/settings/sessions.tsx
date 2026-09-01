import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  LogOut,
  Monitor,
  MoreHorizontal,
  MonitorSmartphone,
  Smartphone,
} from "lucide-react";
import { toast } from "sonner";
import {
  getMySessions,
  revokeAllMySessions,
  revokeMySession,
  type UserSessionDto,
} from "@/api/sessions";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { ErrorBand, LoadingRow, SettingsSection } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";

export function SessionsSettings() {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: ["identity", "sessions", "me"],
    queryFn: getMySessions,
    staleTime: 15_000,
  });

  const sorted = useMemo(() => sortSessions(query.data ?? []), [query.data]);
  const activeOtherCount = sorted.filter((s) => s.isActive && !s.isCurrentSession).length;
  // A Set (not a single id) so concurrent revokes track independently and the
  // first to resolve doesn't clear a still-pending row's busy state.
  const [busyIds, setBusyIds] = useState<ReadonlySet<string>>(() => new Set());

  const revokeOne = useMutation({
    mutationFn: (sessionId: string) => revokeMySession(sessionId),
    onMutate: (sessionId) => setBusyIds((prev) => new Set(prev).add(sessionId)),
    onSuccess: () => {
      toast.success("Сессия отозвана");
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
    },
    onError: (err) => toast.error("Не удалось отозвать", { description: describe(err) }),
    onSettled: (_d, _e, sessionId) =>
      setBusyIds((prev) => {
        const next = new Set(prev);
        next.delete(sessionId);
        return next;
      }),
  });

  const revokeAll = useMutation({
    mutationFn: revokeAllMySessions,
    onSuccess: (data) => {
      toast.success(`Отозвано других сессий: ${data.revokedCount}`);
      void queryClient.invalidateQueries({ queryKey: ["identity", "sessions", "me"] });
    },
    onError: (err) => toast.error("Не удалось отозвать все", { description: describe(err) }),
  });

  if (query.isLoading) return <LoadingRow label="Загрузка сессий" />;
  if (query.isError) {
    return (
      <ErrorBand
        message={
          query.error instanceof ApiRequestError
            ? (query.error.problem?.detail ?? query.error.message)
            : "Не удалось загрузить сессии."
        }
      />
    );
  }

  return (
    <div className="space-y-5 fsh-enter">
      <SettingsSection
        title="Активные сессии"
        icon={MonitorSmartphone}
        description="Все браузеры и устройства, где сейчас выполнен вход в вашу учётку. Отзыв сессии выводит устройство за ~10 секунд."
        footer={
          activeOtherCount > 0 ? (
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-start gap-2 text-xs">
                <AlertTriangle
                  className="mt-0.5 h-3.5 w-3.5 shrink-0 text-[var(--color-warning)]"
                  aria-hidden
                />
                <span className="text-[var(--color-muted-foreground)]">
                  Активных других сессий: {activeOtherCount}.
                  Завершите их все сразу, если подозреваете компрометацию учётки.
                </span>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => revokeAll.mutate()}
                disabled={revokeAll.isPending}
              >
                <LogOut className="mr-1.5 h-3.5 w-3.5" />
                {revokeAll.isPending ? "Выход…" : "Выйти на всех остальных"}
              </Button>
            </div>
          ) : undefined
        }
      >
        {sorted.length === 0 ? (
          <p className="text-sm text-[var(--color-muted-foreground)]">
            Активных сессий не найдено. (Включая эту? Это баг — обновите страницу.)
          </p>
        ) : (
          <ul className="divide-y divide-[var(--color-border)]">
            {sorted.map((s) => (
              <SessionRow
                key={s.id}
                session={s}
                busy={busyIds.has(s.id)}
                onRevoke={() => revokeOne.mutate(s.id)}
              />
            ))}
          </ul>
        )}
      </SettingsSection>
    </div>
  );
}

function SessionRow({
  session,
  busy,
  onRevoke,
}: {
  session: UserSessionDto;
  busy: boolean;
  onRevoke: () => void;
}) {
  const isMobile = (session.deviceType ?? "").toLowerCase().includes("mobile");
  const Icon = isMobile ? Smartphone : Monitor;

  return (
    <li
      className={cn(
        "grid grid-cols-[auto_1fr_auto] items-center gap-4 py-3",
        !session.isActive && "opacity-60",
      )}
    >
      <span
        className={cn(
          "grid h-9 w-9 place-items-center rounded-full ring-1 ring-inset shrink-0",
          session.isCurrentSession
            ? "bg-[var(--color-primary-soft,oklch(from_var(--color-accent-signal)_l_c_h_/_0.12))] text-[var(--color-accent-signal)] ring-[oklch(from_var(--color-accent-signal)_l_c_h_/_0.30)]"
            : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)] ring-[var(--color-border)]",
        )}
      >
        <Icon className="h-4 w-4" />
      </span>
      <div className="min-w-0">
        <div className="flex flex-wrap items-baseline gap-2">
          <span className="truncate text-sm font-medium">{describeDevice(session)}</span>
          {session.isCurrentSession && (
            <Badge variant="brand" className="font-mono uppercase tracking-[0.14em]">
              Это устройство
            </Badge>
          )}
          {!session.isActive && (
            <Badge variant="muted" className="font-mono uppercase tracking-[0.14em]">
              Отозвана
            </Badge>
          )}
        </div>
        <div className="mt-0.5 flex flex-wrap items-baseline gap-x-3 gap-y-0.5 font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
          <span>{session.ipAddress ?? "IP неизвестен"}</span>
          <span>· активность {formatRelative(session.lastActivityAt)}</span>
          <span>· истекает {formatDate(session.expiresAt)}</span>
        </div>
      </div>
      {session.isCurrentSession ? (
        <span className="text-[10.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]/60 flex items-center gap-1">
          <MoreHorizontal className="h-3.5 w-3.5" aria-hidden /> через «Выйти»
        </span>
      ) : session.isActive ? (
        <Button variant="outline" size="sm" onClick={onRevoke} disabled={busy}>
          <LogOut className="mr-1.5 h-3.5 w-3.5" />
          {busy ? "Отзыв…" : "Отозвать"}
        </Button>
      ) : (
        <span aria-hidden />
      )}
    </li>
  );
}

// ─── helpers ─────────────────────────────────────────────────────────────

function sortSessions(rows: UserSessionDto[]): UserSessionDto[] {
  return [...rows].sort((a, b) => {
    if (a.isCurrentSession && !b.isCurrentSession) return -1;
    if (!a.isCurrentSession && b.isCurrentSession) return 1;
    if (a.isActive && !b.isActive) return -1;
    if (!a.isActive && b.isActive) return 1;
    return new Date(b.lastActivityAt).getTime() - new Date(a.lastActivityAt).getTime();
  });
}

function describeDevice(s: UserSessionDto): string {
  const browser = s.browser ?? "Неизвестный браузер";
  const version = s.browserVersion ? ` ${s.browserVersion}` : "";
  const os = s.operatingSystem ?? "неизвестная ОС";
  return `${browser}${version} · ${os}`;
}

function formatDate(value?: string | null): string {
  if (!value) return "—";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString("ru-RU");
}

function formatRelative(value?: string | null): string {
  if (!value) return "—";
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return value;
  const diff = Date.now() - d.getTime();
  const sec = Math.round(diff / 1000);
  if (sec < 60) return `${sec} с назад`;
  const min = Math.round(sec / 60);
  if (min < 60) return `${min} мин назад`;
  const hr = Math.round(min / 60);
  if (hr < 24) return `${hr} ч назад`;
  const day = Math.round(hr / 24);
  if (day < 14) return `${day} дн назад`;
  return d.toLocaleDateString("ru-RU");
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return String(err);
}
