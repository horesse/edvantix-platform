import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useQueries, useQuery } from "@tanstack/react-query";
import {
  ArrowRight,
  Building2,
  CalendarClock,
  FileText,
  Gauge,
  LayoutDashboard,
  Receipt,
  UsersRound,
} from "lucide-react";
import { getTenantStatus, listTenants, type TenantDto } from "@/api/tenants";
import {
  getUsageSnapshots,
  listInvoices,
  type QuotaResource,
  type UsageSnapshotDto,
} from "@/api/billing";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import {
  EntityPageHeader,
  SettingsSection,
  Stat,
  StatStrip,
  ToneIconTile,
  type ToneIconTileTone,
} from "@/components/list";
import { useAuth } from "@/auth/use-auth";
import { cn } from "@/lib/cn";

// How many tenants we'll fan out per-tenant status calls for. The list
// endpoint's projection has no plan, so "schools by plan" needs one
// GetTenantStatus per school — capped so a large instance doesn't storm
// the API from the dashboard.
const STATUS_FANOUT_CAP = 60;
const EXPIRING_WINDOW_DAYS = 45;
const NEAR_LIMIT_RATIO = 0.8;

const GAUGE_RESOURCES: QuotaResource[] = [
  "ActiveStudents",
  "ActiveTeachers",
  "StudyGroups",
  "MonthlySessions",
  "StorageBytes",
];

const RESOURCE_SHORT: Record<string, string> = {
  ActiveStudents: "ученики",
  ActiveTeachers: "преподаватели",
  StudyGroups: "группы",
  MonthlySessions: "занятия/мес",
  StorageBytes: "файлы",
};

function isUnlimited(limit: number): boolean {
  return limit <= 0 || limit >= Number.MAX_SAFE_INTEGER / 2;
}

function daysUntil(iso: string): number {
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return Number.POSITIVE_INFINITY;
  return Math.round((then - Date.now()) / 86_400_000);
}

function formatDate(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleDateString("ru-RU");
}

/**
 * DashboardPage — обзор платформы для оператора. KPI школ + разбивка по
 * тарифам, истекающие подписки и школы у лимитов — из списочных
 * эндпоинтов и снимков использования (готового агрегата платформы нет).
 */
export function DashboardPage() {
  const { user } = useAuth();

  const tenantsQuery = useQuery({
    queryKey: ["tenants", { pageNumber: 1, pageSize: 100 }],
    queryFn: () => listTenants({ pageNumber: 1, pageSize: 100 }),
  });
  const invoicesQuery = useQuery({
    queryKey: ["billing", "invoices", { pageNumber: 1, pageSize: 50 }],
    queryFn: () => listInvoices({ pageNumber: 1, pageSize: 50 }),
  });
  const usageQuery = useQuery({
    queryKey: ["billing", "usage", "all"],
    queryFn: () => getUsageSnapshots(),
  });

  const tenants: TenantDto[] = useMemo(() => tenantsQuery.data?.items ?? [], [tenantsQuery.data]);
  const tenantsTotal = tenantsQuery.data?.totalCount ?? tenants.length;
  const activeCount = tenants.filter((t) => t.isActive).length;

  // Per-tenant status fan-out — only when the instance is small enough.
  const fanOut = tenants.length > 0 && tenants.length <= STATUS_FANOUT_CAP;
  const statusResults = useQueries({
    queries: fanOut
      ? tenants.map((t) => ({
          queryKey: ["tenant", t.id, "status-lite"],
          queryFn: () => getTenantStatus(t.id),
          staleTime: 60_000,
        }))
      : [],
  });
  const statuses = statusResults.map((r) => r.data).filter(Boolean);
  const statusesLoading = fanOut && statusResults.some((r) => r.isLoading);

  const byPlan = useMemo(() => {
    const counts = new Map<string, number>();
    for (const s of statuses) {
      const key = s!.plan?.trim() || "без тарифа";
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }
    return [...counts.entries()].sort((a, b) => b[1] - a[1]);
  }, [statuses]);

  const expiring = useMemo(() => {
    return tenants
      .map((t) => ({ tenant: t, days: daysUntil(t.validUpto) }))
      .filter((x) => Number.isFinite(x.days) && x.days <= EXPIRING_WINDOW_DAYS)
      .sort((a, b) => a.days - b.days);
  }, [tenants]);

  const nameById = useMemo(() => new Map(tenants.map((t) => [t.id, t.name])), [tenants]);

  const nearLimit = useMemo(() => {
    const snaps: UsageSnapshotDto[] = usageQuery.data ?? [];
    // Latest period per tenant.
    const latestByTenant = new Map<string, { year: number; month: number }>();
    for (const s of snaps) {
      const cur = latestByTenant.get(s.tenantId);
      if (!cur || s.periodYear > cur.year || (s.periodYear === cur.year && s.periodMonth > cur.month)) {
        latestByTenant.set(s.tenantId, { year: s.periodYear, month: s.periodMonth });
      }
    }
    const rows: { tenantId: string; resource: QuotaResource; used: number; limit: number; ratio: number }[] = [];
    for (const s of snaps) {
      const latest = latestByTenant.get(s.tenantId);
      if (!latest || s.periodYear !== latest.year || s.periodMonth !== latest.month) continue;
      if (!GAUGE_RESOURCES.includes(s.resource) || isUnlimited(s.limitUnits)) continue;
      const ratio = s.usedUnits / s.limitUnits;
      if (ratio >= NEAR_LIMIT_RATIO) {
        rows.push({ tenantId: s.tenantId, resource: s.resource, used: s.usedUnits, limit: s.limitUnits, ratio });
      }
    }
    return rows.sort((a, b) => b.ratio - a.ratio);
  }, [usageQuery.data]);

  const invoicesPage = invoicesQuery.data;
  const outstandingCount = invoicesPage?.items.filter((i) => i.status === "Issued").length ?? 0;

  const firstName = user?.name?.split(" ")[0];

  return (
    <div className="space-y-6">
      {/* ── Page header ──────────────────────────────────────────────── */}
      <div className="fsh-enter">
        <EntityPageHeader
          icon={LayoutDashboard}
          title={
            <>
              Обзор платформы
              {firstName ? (
                <span className="text-[var(--color-muted-foreground)]">, {firstName}</span>
              ) : null}
            </>
          }
          tone="primary"
          description="Все школы этого экземпляра — идентификация, мультитенантность, биллинг и остальная система."
        />
      </div>

      {/* ── KPI stat strip ───────────────────────────────────────────── */}
      <StatStrip cols={4} className="fsh-enter fsh-enter-2">
        <Stat
          label="Школы"
          value={
            tenantsQuery.isLoading ? <Skeleton className="h-7 w-16" /> : tenantsTotal.toLocaleString("ru-RU")
          }
          hint="зарегистрировано на экземпляре"
        />
        <Stat
          label="Активные"
          value={tenantsQuery.isLoading ? <Skeleton className="h-7 w-16" /> : activeCount.toLocaleString("ru-RU")}
          hint={`неактивных: ${Math.max(tenants.length - activeCount, 0)}`}
        />
        <Stat
          label="Истекают"
          value={tenantsQuery.isLoading ? <Skeleton className="h-7 w-16" /> : expiring.length.toLocaleString("ru-RU")}
          hint={`в ближайшие ${EXPIRING_WINDOW_DAYS} дн.`}
          tone={expiring.length > 0 ? "warning" : "default"}
        />
        <Stat
          label="У лимитов"
          value={usageQuery.isLoading ? <Skeleton className="h-7 w-16" /> : nearLimit.length.toLocaleString("ru-RU")}
          hint={`≥ ${Math.round(NEAR_LIMIT_RATIO * 100)}% лимита тарифа`}
          tone={nearLimit.length > 0 ? "warning" : "default"}
        />
      </StatStrip>

      <div className="grid gap-4 lg:grid-cols-2">
        {/* ── Schools by plan ─────────────────────────────────────────── */}
        <SettingsSection
          icon={Receipt}
          title="Школы по тарифам"
          description={
            fanOut
              ? "Распределение школ по текущему тарифу подписки."
              : `Экземпляр крупнее ${STATUS_FANOUT_CAP} школ — разбивка по тарифам скрыта, чтобы не нагружать API с дашборда.`
          }
        >
          {!fanOut ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Откройте раздел «Школы», чтобы посмотреть тариф каждой.
            </p>
          ) : statusesLoading ? (
            <div className="space-y-2">
              {[0, 1, 2].map((i) => (
                <Skeleton key={i} className="h-6 w-full" />
              ))}
            </div>
          ) : byPlan.length === 0 ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">Нет данных о тарифах.</p>
          ) : (
            <ul className="space-y-2">
              {byPlan.map(([plan, count]) => {
                const pct = statuses.length ? Math.round((count / statuses.length) * 100) : 0;
                return (
                  <li key={plan} className="flex items-center gap-3">
                    <code className="w-28 shrink-0 truncate rounded bg-[var(--color-surface-2)] px-1.5 py-0.5 font-mono text-[11px]">
                      {plan}
                    </code>
                    <div className="h-2 flex-1 overflow-hidden rounded-full bg-[var(--color-muted)]">
                      <div
                        className="h-full rounded-full bg-[var(--color-primary)]"
                        style={{ width: `${pct}%` }}
                      />
                    </div>
                    <span className="w-16 shrink-0 text-right font-mono text-[12px] tabular-nums text-[var(--color-muted-foreground)]">
                      {count} · {pct}%
                    </span>
                  </li>
                );
              })}
            </ul>
          )}
        </SettingsSection>

        {/* ── Expiring subscriptions ──────────────────────────────────── */}
        <SettingsSection
          icon={CalendarClock}
          title="Истекающие подписки"
          description={`Школы, у которых срок заканчивается в ближайшие ${EXPIRING_WINDOW_DAYS} дней или уже истёк.`}
        >
          {tenantsQuery.isLoading ? (
            <div className="space-y-2">
              {[0, 1, 2].map((i) => (
                <Skeleton key={i} className="h-8 w-full" />
              ))}
            </div>
          ) : expiring.length === 0 ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Нет школ с истекающим сроком в этом окне.
            </p>
          ) : (
            <ul className="-mx-2 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
              {expiring.slice(0, 8).map(({ tenant, days }) => (
                <li key={tenant.id}>
                  <Link
                    to={`/tenants/${tenant.id}`}
                    className="flex items-center justify-between gap-3 rounded-md px-2 py-2 text-[13px] transition-colors hover:bg-[var(--color-muted)]"
                  >
                    <span className="min-w-0 truncate font-medium text-[var(--color-foreground)]">
                      {tenant.name}
                    </span>
                    <span className="flex shrink-0 items-center gap-2">
                      <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                        {formatDate(tenant.validUpto)}
                      </span>
                      <Badge variant={days < 0 ? "danger" : "warning"}>
                        {days < 0 ? `истёк ${-days} дн. назад` : days === 0 ? "сегодня" : `через ${days} дн.`}
                      </Badge>
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </SettingsSection>
      </div>

      {/* ── Near-limit schools ───────────────────────────────────────── */}
      <SettingsSection
        icon={Gauge}
        title="Приближаются к лимитам"
        description={`Школы, где использование ресурса достигло ${Math.round(
          NEAR_LIMIT_RATIO * 100,
        )}% лимита тарифа (по последнему снимку биллинга).`}
      >
        {usageQuery.isLoading ? (
          <div className="space-y-2">
            {[0, 1].map((i) => (
              <Skeleton key={i} className="h-8 w-full" />
            ))}
          </div>
        ) : nearLimit.length === 0 ? (
          <p className="text-[13px] text-[var(--color-muted-foreground)]">
            Нет школ у лимитов, либо снимки использования ещё не захвачены.
          </p>
        ) : (
          <ul className="-mx-2 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
            {nearLimit.slice(0, 10).map((row, i) => (
              <li key={`${row.tenantId}-${row.resource}-${i}`}>
                <Link
                  to={`/tenants/${row.tenantId}`}
                  className="flex items-center justify-between gap-3 rounded-md px-2 py-2 text-[13px] transition-colors hover:bg-[var(--color-muted)]"
                >
                  <span className="min-w-0 truncate font-medium text-[var(--color-foreground)]">
                    {nameById.get(row.tenantId) ?? row.tenantId}
                  </span>
                  <span className="flex shrink-0 items-center gap-2 font-mono text-[11px] text-[var(--color-muted-foreground)]">
                    <span>{RESOURCE_SHORT[row.resource] ?? row.resource}</span>
                    <Badge variant={row.ratio >= 1 ? "danger" : "warning"}>
                      {row.used.toLocaleString("ru-RU")} / {row.limit.toLocaleString("ru-RU")} ·{" "}
                      {Math.round(row.ratio * 100)}%
                    </Badge>
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </SettingsSection>

      {/* ── Quick pivots ─────────────────────────────────────────────── */}
      <section className="fsh-enter fsh-enter-3 space-y-3">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
          Точки входа
        </p>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <PivotCard
            to="/tenants"
            icon={Building2}
            tone="info"
            title="Школы"
            description="Заводить, блокировать и разбирать школы."
          />
          <PivotCard
            to="/users"
            icon={UsersRound}
            tone="primary"
            title="Пользователи"
            description="Операторы корневой школы и управление ролями."
          />
          <PivotCard
            to="/billing/plans"
            icon={Receipt}
            tone="success"
            title="Биллинг"
            description="Тарифы, подписки, счета и цены."
          />
          <PivotCard
            to="/billing/invoices"
            icon={FileText}
            tone="warning"
            title="Счета"
            description={`Реестр по всем школам. К оплате: ${outstandingCount}.`}
          />
        </div>
      </section>
    </div>
  );
}

// ─── subcomponents ───────────────────────────────────────────────────

function PivotCard({
  to,
  icon: Icon,
  tone,
  title,
  description,
}: {
  to: string;
  icon: typeof Building2;
  tone: ToneIconTileTone;
  title: string;
  description: string;
}) {
  return (
    <Link to={to} className="group block focus:outline-none">
      <div
        className={cn(
          "flex h-full flex-col gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs",
          "transition-colors duration-200 hover:border-[var(--color-border-strong)] hover:bg-[var(--color-accent)]",
        )}
      >
        <div className="flex items-start justify-between">
          <ToneIconTile icon={Icon} tone={tone} size="md" />
          <ArrowRight
            aria-hidden
            className="size-3.5 text-[var(--color-muted-foreground)] opacity-0 transition-all duration-200 group-hover:translate-x-0.5 group-hover:opacity-100"
          />
        </div>
        <div>
          <div className="font-display text-[14px] font-semibold tracking-tight text-[var(--color-foreground)]">
            {title}
          </div>
          <p className="mt-0.5 text-[12px] leading-snug text-[var(--color-muted-foreground)]">
            {description}
          </p>
        </div>
      </div>
    </Link>
  );
}
