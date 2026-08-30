import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { TrendingUp } from "lucide-react";
import { getRevenueReport } from "@/api/payments";
import { useAuth } from "@/auth/use-auth";
import { Input } from "@/components/ui/input";
import { EntityEmpty, ErrorBand, Field, PageHero } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { PAYMENT_METHOD_LABEL } from "./payments-ui";

function firstOfMonth() {
  const d = new Date();
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
}

export function RevenuePage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Payments.StudentInvoices.Export");

  const [periodFrom, setPeriodFrom] = useState(firstOfMonth());
  const [periodTo, setPeriodTo] = useState(new Date().toISOString().slice(0, 10));

  const valid = periodFrom.length > 0 && periodTo.length > 0 && periodFrom <= periodTo;

  const query = useQuery({
    queryKey: ["payments-revenue", { periodFrom, periodTo }],
    queryFn: () => getRevenueReport(periodFrom, periodTo),
    enabled: canView && valid,
  });

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Оплаты" title="Поступления" />
        <EntityEmpty
          icon={TrendingUp}
          title="Нет доступа"
          body="Нужно право «Экспорт счетов учеников»."
        />
      </div>
    );
  }

  const report = query.data;
  const max = report
    ? Math.max(1, ...report.byMethod.map((m) => Math.abs(m.amount)))
    : 1;

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Оплаты"
        title="Поступления"
        subtitle="Сумма подтверждённых оплат за период с разбивкой по способу. Сторно учтено автоматически (суммы со знаком)."
      />

      <div className="flex flex-wrap items-end gap-3">
        <Field id="rv-from" label="Период с" required>
          <Input
            id="rv-from"
            type="date"
            value={periodFrom}
            onChange={(e) => setPeriodFrom(e.target.value)}
            className="w-[11rem]"
          />
        </Field>
        <Field id="rv-to" label="Период по" required>
          <Input
            id="rv-to"
            type="date"
            value={periodTo}
            onChange={(e) => setPeriodTo(e.target.value)}
            className="w-[11rem]"
          />
        </Field>
      </div>

      {!valid && (
        <p className="text-[12px] text-[var(--color-destructive)]">
          «Период по» не может быть раньше «Период с».
        </p>
      )}

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : report ? (
        <div className="space-y-4">
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-5 py-4">
            <p className="text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              Всего за {formatDate(report.periodFrom)} — {formatDate(report.periodTo)}
            </p>
            <p className="mt-1 font-display text-[26px] font-bold tabular-nums text-[var(--color-foreground)]">
              {report.total.toFixed(2)}
            </p>
          </div>

          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-5">
            <h2 className="mb-3 text-[13px] font-semibold text-[var(--color-foreground)]">
              По способу оплаты
            </h2>
            {report.byMethod.length === 0 ? (
              <p className="text-[13px] text-[var(--color-muted-foreground)]">
                За этот период поступлений нет.
              </p>
            ) : (
              <ul className="space-y-3">
                {report.byMethod.map((m) => (
                  <li key={m.method} className="space-y-1">
                    <div className="flex items-center justify-between text-[13px]">
                      <span className="text-[var(--color-foreground)]">
                        {PAYMENT_METHOD_LABEL[m.method]}
                      </span>
                      <span className="tabular-nums font-medium text-[var(--color-foreground)]">
                        {m.amount.toFixed(2)}
                      </span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-full bg-[var(--color-muted)]">
                      <div
                        className="h-full rounded-full bg-[var(--color-primary)]"
                        style={{
                          width: `${Math.max(2, (Math.abs(m.amount) / max) * 100)}%`,
                        }}
                      />
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
