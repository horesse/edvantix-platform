import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Pencil, Plus, Tag } from "lucide-react";
import {
  getPlans,
  planTermPrice,
  type BillingPlanDto,
  type QuotaResource,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { StatStrip, Stat, SettingsSection } from "@/components/list";
import { PlanFormDialog } from "@/components/billing/plan-form-dialog";
import { ApiRequestError } from "@/lib/api-client";
import { useAuth } from "@/auth/use-auth";
import { BillingPermissions } from "@/lib/permissions";

// ─── helpers ──────────────────────────────────────────────────────────

/** School-term labels for quota resources — "Ученики" not "ActiveStudents". */
const RESOURCE_LABEL: Record<string, string> = {
  ActiveStudents: "ученики",
  ActiveTeachers: "преподаватели",
  StudyGroups: "учебные группы",
  MonthlySessions: "занятия/мес",
  StorageBytes: "объём файлов",
  ApiCalls: "вызовы API",
  Users: "учётные записи",
  ActiveFeatureFlags: "флаги функций",
};

function resourceLabel(resource: string): string {
  return RESOURCE_LABEL[resource] ?? resource;
}

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat("ru-RU", { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

function formatOverageRates(rates: BillingPlanDto["overageRates"], currency: string) {
  const entries = Object.entries(rates).filter(([, v]) => v && v > 0) as [QuotaResource, number][];
  if (entries.length === 0) return "—";
  return entries
    .map(([resource, rate]) => `${resourceLabel(resource)} ${formatMoney(rate ?? 0, currency)}`)
    .join(" · ");
}

function describe(err: unknown): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return "Не удалось загрузить тарифы.";
}

// ─── component ────────────────────────────────────────────────────────

export function PlansListPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingPlan, setEditingPlan] = useState<BillingPlanDto | undefined>(undefined);
  const { user: currentUser } = useAuth();
  const canManageBilling = (currentUser?.permissions ?? []).includes(BillingPermissions.Manage);

  const openCreate = () => {
    setEditingPlan(undefined);
    setDialogOpen(true);
  };
  const openEdit = (plan: BillingPlanDto) => {
    setEditingPlan(plan);
    setDialogOpen(true);
  };

  const query = useQuery({
    queryKey: ["billing", "plans", { includeInactive: true }],
    queryFn: () => getPlans(true),
  });

  const plans = useMemo<BillingPlanDto[]>(() => query.data ?? [], [query.data]);

  const totals = useMemo(() => {
    if (plans.length === 0) {
      return { count: 0, active: 0, averagePrice: 0, currency: "USD" };
    }
    const active = plans.filter((p) => p.isActive).length;
    const sum = plans.reduce((acc, p) => acc + p.monthlyBasePrice, 0);
    return {
      count: plans.length,
      active,
      averagePrice: sum / plans.length,
      currency: plans[0].currency,
    };
  }, [plans]);

  return (
    <div className="space-y-6">
      {/* KPI strip */}
      <StatStrip cols={3}>
        <Stat
          label="Тарифы"
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : totals.count}
          hint={`активных: ${totals.active}`}
        />
        <Stat
          label="Активные"
          value={query.isLoading ? <Skeleton className="h-7 w-16" /> : totals.active}
          hint={totals.count - totals.active > 0 ? `неактивных: ${totals.count - totals.active}` : "все активны"}
        />
        <Stat
          label="Средняя база"
          value={
            query.isLoading ? (
              <Skeleton className="h-7 w-24" />
            ) : (
              formatMoney(totals.averagePrice, totals.currency)
            )
          }
          hint="месячная плата за подписку"
        />
      </StatStrip>

      {/* Plans list */}
      <SettingsSection
        icon={Tag}
        title="Все тарифы"
        description="Прайс-лист для подписок школ и генерации счетов платформы. Лимиты — в учениках и преподавателях."
        footer={
          canManageBilling ? (
            <Button onClick={openCreate}>
              <Plus className="mr-1 h-4 w-4" /> Новый тариф
            </Button>
          ) : undefined
        }
      >
        {query.isError && (
          <div className="mb-4 rounded-md border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.05)] px-4 py-3 text-sm text-[var(--color-destructive)]">
            {describe(query.error)}
          </div>
        )}

        {query.isLoading ? (
          <ul className="-mx-5 divide-y divide-[var(--color-border)] border-t border-[var(--color-border)]">
            {Array.from({ length: 3 }).map((_, i) => (
              <li key={i} className="px-5 py-5">
                <Skeleton className="h-5 w-1/3" />
                <Skeleton className="mt-2 h-3 w-1/2" />
              </li>
            ))}
          </ul>
        ) : plans.length === 0 ? (
          <div className="py-10 text-center text-sm text-[var(--color-muted-foreground)]">
            Тарифов пока нет. Создайте первый, чтобы начать выставлять счета школам.
          </div>
        ) : (
          <ul className="-mx-5 border-t border-[var(--color-border)]">
            {plans.map((plan, i) => (
              <li
                key={plan.id}
                className="fsh-enter grid grid-cols-[1fr_auto] items-center gap-x-6 gap-y-1 border-b border-[var(--color-border)] last:border-b-0 px-5 py-4 transition-colors hover:bg-[var(--color-muted)]"
                style={{ animationDelay: `${Math.min(i, 6) * 30}ms` }}
              >
                {/* Identity column */}
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <code className="rounded bg-[var(--color-surface-2)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight">
                      {plan.key}
                    </code>
                    <span className="font-display text-base font-semibold">{plan.name}</span>
                    <Badge variant="outline">{plan.interval === "Yearly" ? "Год" : "Месяц"}</Badge>
                    {plan.isActive ? (
                      <Badge variant="success">Активен</Badge>
                    ) : (
                      <Badge variant="muted">Неактивен</Badge>
                    )}
                  </div>
                  {plan.description && (
                    <p className="mt-1 text-[12px] leading-snug text-[var(--color-muted-foreground)]">
                      {plan.description}
                    </p>
                  )}
                  <div className="mt-1 font-mono text-[11px] tracking-tight text-[var(--color-muted-foreground)]">
                    валюта {plan.currency} · превышение{" "}
                    {formatOverageRates(plan.overageRates, plan.currency)}
                  </div>
                </div>

                {/* Right column — price + edit */}
                <div className="flex items-center gap-4">
                  <div className="text-right">
                    <div className="text-display text-lg font-semibold leading-none tabular-nums">
                      {formatMoney(planTermPrice(plan), plan.currency)}
                    </div>
                    <div className="mt-1 font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                      {plan.interval === "Yearly" ? "за год" : "за месяц"}
                    </div>
                  </div>
                  {canManageBilling && (
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Изменить тариф «${plan.name}»`}
                      onClick={() => openEdit(plan)}
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </SettingsSection>

      <PlanFormDialog open={dialogOpen} onOpenChange={setDialogOpen} plan={editingPlan} />
    </div>
  );
}
