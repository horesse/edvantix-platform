import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ChevronRight, TriangleAlert } from "lucide-react";
import { getDebtorsReport } from "@/api/payments";
import { searchStudents } from "@/api/people";
import { searchStudyGroups } from "@/api/study-groups";
import { useAuth } from "@/auth/use-auth";
import {
  Combobox,
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListRow,
  ErrorBand,
  PageHero,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";

const DESKTOP_COLS = "grid-cols-[1fr_120px_90px_130px_24px]";

export function DebtorsPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Payments.StudentInvoices.Export");

  const [groupFilter, setGroupFilter] = useState<string | null>(null);

  const query = useQuery({
    queryKey: ["payments-debtors", { groupFilter }],
    queryFn: () => getDebtorsReport(groupFilter),
    enabled: canView,
  });

  const studentsQuery = useQuery({
    queryKey: ["students", { pageSize: 100, for: "debtors" }],
    queryFn: () => searchStudents({ pageSize: 100 }),
    staleTime: 60_000,
    enabled: canView,
  });
  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "debtors" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
    enabled: canView,
  });

  const studentName = useMemo(() => {
    const m = new Map<string, string>();
    for (const s of studentsQuery.data?.items ?? []) m.set(s.id, s.displayName);
    return m;
  }, [studentsQuery.data]);
  const groupOptions = useMemo(
    () =>
      (groupsQuery.data?.items ?? []).map((g) => ({
        value: g.id,
        label: `${g.name} · ${g.code}`,
      })),
    [groupsQuery.data],
  );

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Оплаты" title="Должники" />
        <EntityEmpty
          icon={TriangleAlert}
          title="Нет доступа"
          body="Нужно право «Экспорт счетов учеников»."
        />
      </div>
    );
  }

  const rows = [...(query.data ?? [])].sort((a, b) => b.debt - a.debt);
  const totalDebt = rows.reduce((s, d) => s + d.debt, 0);

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Оплаты"
        title="Должники"
        subtitle="Ученики с просроченными выставленными счетами. Сумма долга — по всем просроченным счетам ученика."
        badge={
          rows.length > 0 ? (
            <span className="font-mono text-[13px] text-[var(--color-muted-foreground)]">
              {rows.length} · {totalDebt.toFixed(2)}
            </span>
          ) : undefined
        }
      />

      <Combobox
        label="Группа"
        value={groupFilter}
        onChange={setGroupFilter}
        options={groupOptions}
        variant="filter"
        searchable
        clearable
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : rows.length === 0 ? (
        <EntityEmpty
          icon={TriangleAlert}
          title="Должников нет"
          body="Ни по одному ученику нет просроченных счетов."
        />
      ) : (
        <EntityListCard>
          <EntityListHeader className={DESKTOP_COLS}>
            <span>Ученик</span>
            <span className="text-right">Долг</span>
            <span className="text-right">Счетов</span>
            <span className="text-right">Старейший срок</span>
            <span />
          </EntityListHeader>
          {rows.map((d, i) => (
            <EntityListRow
              key={d.studentId}
              className={DESKTOP_COLS}
              isLast={i === rows.length - 1}
            >
              <Link
                to={`/students/${d.studentId}`}
                className="truncate text-[13px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]"
              >
                {studentName.get(d.studentId) ?? d.studentId}
              </Link>
              <span className="text-right tabular-nums text-[13px] font-semibold text-[var(--color-destructive)]">
                {d.debt.toFixed(2)}
              </span>
              <span className="text-right tabular-nums text-[12px] text-[var(--color-muted-foreground)]">
                {d.overdueInvoiceCount}
              </span>
              <span className="text-right tabular-nums text-[12px] text-[var(--color-muted-foreground)]">
                {formatDate(d.oldestDueDate)}
              </span>
              <div className="flex items-center justify-end">
                <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
              </div>
            </EntityListRow>
          ))}
        </EntityListCard>
      )}
    </div>
  );
}
