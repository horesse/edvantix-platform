import { useQuery } from "@tanstack/react-query";
import { BookOpen, GraduationCap, HardDrive, LineChart, Users2 } from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { getUsageSnapshots, type QuotaResource, type UsageSnapshotDto } from "@/api/billing";
import { ApiRequestError } from "@/lib/api-client";
import { SettingsSection } from "@/components/list";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/cn";

// The school-card metrics: the domain quota gauges (students / teachers /
// groups / monthly sessions) plus storage volume. Sourced from Billing's
// per-period usage snapshots (GET /billing/usage) — the only cross-tenant
// aggregate the platform exposes. There is no live "current" endpoint, so
// this shows the most recent captured period and says so.

const METRIC_ORDER: { resource: QuotaResource; label: string; icon: LucideIcon }[] = [
  { resource: "ActiveStudents", label: "Активные ученики", icon: Users2 },
  { resource: "ActiveTeachers", label: "Преподаватели", icon: GraduationCap },
  { resource: "StudyGroups", label: "Учебные группы", icon: BookOpen },
  { resource: "MonthlySessions", label: "Занятий за месяц", icon: LineChart },
  { resource: "StorageBytes", label: "Объём файлов", icon: HardDrive },
];

const MONTHS_RU = [
  "январь", "февраль", "март", "апрель", "май", "июнь",
  "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь",
];

/** A limit of -1 (or an absurdly large sentinel) means "unlimited". */
function isUnlimited(limit: number): boolean {
  return limit < 0 || limit >= Number.MAX_SAFE_INTEGER / 2;
}

function formatBytes(bytes: number): string {
  if (bytes <= 0) return "0 Б";
  const units = ["Б", "КБ", "МБ", "ГБ", "ТБ"];
  const exp = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  const value = bytes / Math.pow(1024, exp);
  return `${value.toFixed(value < 10 && exp > 0 ? 1 : 0)} ${units[exp]}`;
}

function formatUsed(resource: QuotaResource, used: number): string {
  return resource === "StorageBytes" ? formatBytes(used) : used.toLocaleString("ru-RU");
}

function formatLimit(resource: QuotaResource, limit: number): string {
  if (isUnlimited(limit)) return "без лимита";
  return resource === "StorageBytes" ? formatBytes(limit) : limit.toLocaleString("ru-RU");
}

export function SchoolMetricsCard({ tenantId, canView }: { tenantId: string; canView: boolean }) {
  const query = useQuery({
    queryKey: ["billing", "usage", tenantId],
    queryFn: () => getUsageSnapshots({ tenantId }),
    enabled: canView && !!tenantId,
  });

  // The endpoint is Billing.View-gated; a caller without it just doesn't see
  // the card (no 403 dead-end).
  if (!canView) return null;

  const snapshots: UsageSnapshotDto[] = query.data ?? [];
  // Snapshots come newest-period first; the first row's period is the latest.
  const latest = snapshots[0];
  const period = latest
    ? snapshots.filter(
        (s) => s.periodYear === latest.periodYear && s.periodMonth === latest.periodMonth,
      )
    : [];
  const byResource = new Map(period.map((s) => [s.resource, s]));

  return (
    <SettingsSection
      title="Показатели школы"
      icon={LineChart}
      description={
        latest
          ? `Снимок за ${MONTHS_RU[latest.periodMonth - 1]} ${latest.periodYear}. Обновляется ежемесячным заданием биллинга.`
          : "Доменные метрики школы из снимков использования биллинга."
      }
    >
      {query.isLoading ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          {METRIC_ORDER.map((m) => (
            <Skeleton key={m.resource} className="h-[86px] rounded-xl" />
          ))}
        </div>
      ) : query.isError ? (
        <p className="text-[13px] text-[var(--color-destructive)]">
          {query.error instanceof ApiRequestError
            ? query.error.problem?.detail ?? query.error.message
            : "Не удалось загрузить показатели школы."}
        </p>
      ) : period.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)] px-4 py-3.5 text-[13px] leading-relaxed text-[var(--color-muted-foreground)]">
          Снимков использования для этой школы пока нет. Они появятся после первого
          прогона ежемесячного задания биллинга (или ручного захвата через
          <code className="mx-1 rounded bg-[var(--color-surface-2)] px-1 py-0.5 font-mono text-[11px]">
            POST /billing/usage/snapshots/capture
          </code>
          ).
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
          {METRIC_ORDER.map(({ resource, label, icon: Icon }) => {
            const snap = byResource.get(resource);
            const over = snap ? snap.overage > 0 : false;
            return (
              <div
                key={resource}
                className="flex flex-col gap-1.5 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-3.5 py-3 shadow-xs"
              >
                <div className="flex items-center gap-1.5 text-[11px] font-medium text-[var(--color-muted-foreground)]">
                  <Icon aria-hidden className="size-3.5 shrink-0" />
                  <span className="truncate">{label}</span>
                </div>
                <div
                  className={cn(
                    "text-display text-[22px] font-semibold leading-none tabular-nums",
                    over ? "text-[var(--color-warning)]" : "text-[var(--color-foreground)]",
                  )}
                >
                  {snap ? formatUsed(resource, snap.usedUnits) : "—"}
                </div>
                <div className="text-[11px] text-[var(--color-muted-foreground)]">
                  {snap ? `лимит: ${formatLimit(resource, snap.limitUnits)}` : "нет данных"}
                  {over && (
                    <span className="ml-1 text-[var(--color-warning)]">
                      · сверх лимита {formatUsed(resource, snap!.overage)}
                    </span>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </SettingsSection>
  );
}
