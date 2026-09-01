import { useMemo } from "react";
import { Link, Navigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  ArrowRight,
  CalendarRange,
  Receipt,
  UsersRound,
} from "lucide-react";
import { getMySchedule } from "@/api/scheduling";
import { getMyStudyGroups } from "@/api/study-groups";
import { getMyInvoices, outstanding } from "@/api/payments";
import { getTenantSettings } from "@/api/tenant-settings";
import { useAuth } from "@/auth/use-auth";
import { useCabinetRole, isCabinetRole } from "@/cabinet/use-cabinet-role";
import { useWard } from "@/cabinet/use-ward";
import { WardSwitcher } from "@/cabinet/ward-switcher";
import { EntityDetailSection, PageHero } from "@/components/list";
import { cn } from "@/lib/cn";
import { formatZonedDateTime } from "@/lib/tz";
import { formatMoney } from "@/pages/payments/payments-ui";
import {
  SESSION_STATUS_LABEL,
} from "@/pages/scheduling/scheduling-ui";
import { STATUS_LABEL } from "@/pages/study-groups/study-groups-ui";

// ─────────────────────────────────────────────────────────────────────────
//  `/my` — стартовая страница кабинета для преподавателя / ученика /
//  представителя. Менеджер/админ сюда не попадает: индексный редирект
//  ведёт его на обзор школы `/`, а прямой заход на `/my` мы разворачиваем
//  обратно.
// ─────────────────────────────────────────────────────────────────────────

function firstNameOf(name: string | undefined, email: string | undefined): string {
  const base = (name ?? email?.split("@")[0] ?? "").trim();
  return base.split(/\s+/)[0] || "";
}

function useHorizon(days: number) {
  return useMemo(() => {
    const start = new Date();
    start.setHours(0, 0, 0, 0);
    const end = new Date(start.getTime() + days * 24 * 60 * 60 * 1000);
    return { from: start.toISOString(), to: end.toISOString() };
  }, [days]);
}

function UpcomingSessions({ studentId }: { studentId: string | null }) {
  const { from, to } = useHorizon(7);
  const settingsQuery = useQuery({
    queryKey: ["tenant-settings"],
    queryFn: getTenantSettings,
    staleTime: 5 * 60_000,
  });
  const tz = settingsQuery.data?.timeZoneId || "UTC";

  const query = useQuery({
    queryKey: ["sessions", "my", { from, to, ward: studentId }],
    queryFn: () => getMySchedule(from, to, studentId),
  });
  const items = useMemo(
    () =>
      [...(query.data ?? [])]
        .sort((a, b) => a.startUtc.localeCompare(b.startUtc))
        .slice(0, 5),
    [query.data],
  );

  if (query.isLoading) {
    return <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>;
  }
  if (items.length === 0) {
    return (
      <p className="text-sm text-[var(--color-muted-foreground)]">
        На ближайшую неделю занятий нет.
      </p>
    );
  }
  return (
    <ul className="-my-1 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
      {items.map((s) => (
        <li key={s.id} className="py-2.5">
          <Link
            to={`/sessions/${s.id}`}
            className="flex items-center justify-between gap-3 text-[13px]"
          >
            <span className="min-w-0 truncate text-[var(--color-foreground)]">
              {s.topic || "Занятие"}
            </span>
            <span className="shrink-0 tabular-nums text-[11.5px] text-[var(--color-muted-foreground)]">
              {formatZonedDateTime(s.startUtc, tz)} · {SESSION_STATUS_LABEL[s.status]}
            </span>
          </Link>
        </li>
      ))}
    </ul>
  );
}

function MyGroupsPreview() {
  const query = useQuery({ queryKey: ["my-study-groups"], queryFn: getMyStudyGroups });
  const items = (query.data ?? []).slice(0, 5);
  if (query.isLoading) {
    return <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>;
  }
  if (items.length === 0) {
    return <p className="text-sm text-[var(--color-muted-foreground)]">Групп пока нет.</p>;
  }
  return (
    <ul className="-my-1 divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
      {items.map((g) => (
        <li key={g.id} className="py-2.5">
          <Link
            to={`/study-groups/${g.id}`}
            className="flex items-center justify-between gap-3 text-[13px]"
          >
            <span className="min-w-0 truncate text-[var(--color-foreground)]">
              <span className="font-mono text-[12px] text-[var(--color-muted-foreground)]">
                {g.code}
              </span>{" "}
              {g.name}
            </span>
            <span className="shrink-0 text-[11.5px] text-[var(--color-muted-foreground)]">
              {STATUS_LABEL[g.status]}
            </span>
          </Link>
        </li>
      ))}
    </ul>
  );
}

function InvoicesSummary({ canView }: { canView: boolean }) {
  const query = useQuery({
    queryKey: ["my-invoices", { status: "all" }],
    queryFn: () => getMyInvoices(null),
    enabled: canView,
  });
  const summary = useMemo(() => {
    const list = query.data ?? [];
    const due = list.reduce((sum, inv) => sum + outstanding(inv), 0);
    const currency = list.find((inv) => inv.currency)?.currency ?? "RUB";
    const overdue = list.filter((inv) => inv.isOverdue).length;
    return { due, currency, overdue, count: list.length };
  }, [query.data]);

  if (!canView) {
    return (
      <p className="text-sm text-[var(--color-muted-foreground)]">
        Нет права на просмотр своих счетов.
      </p>
    );
  }
  if (query.isLoading) {
    return <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>;
  }
  return (
    <div className="space-y-1.5 text-[13px]">
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-[var(--color-muted-foreground)]">К оплате</span>
        <span className="font-display text-[18px] font-bold tabular-nums text-[var(--color-foreground)]">
          {formatMoney(summary.due, summary.currency)}
        </span>
      </div>
      <div className="flex items-baseline justify-between gap-3">
        <span className="text-[var(--color-muted-foreground)]">Всего счетов</span>
        <span className="tabular-nums text-[var(--color-foreground)]">{summary.count}</span>
      </div>
      {summary.overdue > 0 && (
        <div className="flex items-baseline justify-between gap-3">
          <span className="text-[var(--color-muted-foreground)]">Просрочено</span>
          <span className="tabular-nums text-[var(--color-destructive)]">
            {summary.overdue}
          </span>
        </div>
      )}
    </div>
  );
}

function CabinetLink({
  to,
  icon: Icon,
  title,
  hint,
}: {
  to: string;
  icon: React.ComponentType<{ className?: string }>;
  title: string;
  hint: string;
}) {
  return (
    <Link
      to={to}
      className={cn(
        "group flex items-start gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-4 py-3.5 shadow-xs",
        "transition-colors hover:border-[var(--color-border-strong)]",
      )}
    >
      <span
        aria-hidden
        className="grid size-9 shrink-0 place-items-center rounded-lg bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]"
      >
        <Icon className="size-4" />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-[13px] font-semibold text-[var(--color-foreground)]">{title}</p>
        <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">{hint}</p>
      </div>
      <ArrowRight className="size-3.5 shrink-0 text-[var(--color-muted-foreground)] opacity-0 transition-opacity group-hover:opacity-100" />
    </Link>
  );
}

export function CabinetLandingPage() {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  const { role, isLoading } = useCabinetRole();
  const { selectedWardId, selectedWard } = useWard();

  if (isLoading) {
    return (
      <div
        className="flex min-h-[40vh] items-center justify-center"
        role="status"
        aria-busy="true"
      >
        <span className="sr-only">Загрузка кабинета…</span>
        <span
          className="size-5 animate-spin rounded-full border-2 border-current border-t-transparent text-[var(--color-muted-foreground)]"
          aria-hidden
        />
      </div>
    );
  }

  // Менеджер/админ/неопознанный — их место на обзоре школы.
  if (!isCabinetRole(role)) {
    return <Navigate to="/" replace />;
  }

  const firstName = firstNameOf(user?.name, user?.email);
  const isTeacher = role === "teacher";
  const canViewInvoices = perms.includes("Permissions.Payments.StudentInvoices.ViewOwn");

  return (
    <div className="space-y-5">
      <PageHero
        eyebrow="Кабинет"
        title={firstName ? `Здравствуйте, ${firstName}` : "Ваш кабинет"}
        subtitle={
          isTeacher
            ? "Ваши занятия и группы на ближайшее время."
            : selectedWard
              ? `Расписание и счета подопечного «${selectedWard.name}».`
              : "Расписание и счета — ваши и ваших подопечных."
        }
      />

      {!isTeacher && <WardSwitcher />}

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <CabinetLink
          to="/my/schedule"
          icon={CalendarRange}
          title="Моё расписание"
          hint="Ближайшие занятия"
        />
        <CabinetLink
          to="/my/groups"
          icon={UsersRound}
          title="Мои группы"
          hint="Группы, где вы состоите"
        />
        {!isTeacher && (
          <CabinetLink
            to="/my/invoices"
            icon={Receipt}
            title="Мои счета"
            hint="Счета за обучение"
          />
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <EntityDetailSection title="Ближайшие занятия" icon={CalendarRange}>
          <UpcomingSessions studentId={isTeacher ? null : selectedWardId} />
        </EntityDetailSection>

        {isTeacher ? (
          <EntityDetailSection title="Мои группы" icon={UsersRound}>
            <MyGroupsPreview />
          </EntityDetailSection>
        ) : (
          <EntityDetailSection title="Мои счета" icon={Receipt}>
            <InvoicesSummary canView={canViewInvoices} />
          </EntityDetailSection>
        )}
      </div>
    </div>
  );
}
