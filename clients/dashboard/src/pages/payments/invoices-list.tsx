import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { ChevronRight, FileText, Layers, Receipt } from "lucide-react";
import { toast } from "sonner";
import {
  bulkGenerateInvoices,
  bulkIssueInvoices,
  searchStudentInvoices,
  type BulkGenerateInput,
  type StudentInvoiceDto,
  type InvoiceStatus,
} from "@/api/payments";
import { searchStudents } from "@/api/people";
import { searchStudyGroups } from "@/api/study-groups";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import {
  formatMoney,
  INVOICE_STATUS_LABEL,
  INVOICE_STATUS_TONE,
} from "./payments-ui";

const PAGE_SIZE = 20;
const today = () => new Date().toISOString().slice(0, 10);

type StatusFilter = InvoiceStatus | "all";
type DebtFilter = "all" | "debt";
type SortKey = "number" | "dueDate" | "total" | "status";

const DESKTOP_COLS =
  "grid-cols-[28px_1fr_110px_24px] lg:grid-cols-[28px_1.4fr_150px_120px_120px_110px_24px]";

export function InvoicesListPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Payments.StudentInvoices.View");
  const canCreate = perms.includes("Permissions.Payments.StudentInvoices.Create");
  const canIssue = perms.includes("Permissions.Payments.StudentInvoices.Issue");
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [debtFilter, setDebtFilter] = useState<DebtFilter>("all");
  const [studentFilter, setStudentFilter] = useState<string | null>(null);
  const [groupFilter, setGroupFilter] = useState<string | null>(null);
  const [periodFrom, setPeriodFrom] = useState("");
  const [periodTo, setPeriodTo] = useState("");
  const [sortBy, setSortBy] = useState<SortKey>("number");
  const [wizardOpen, setWizardOpen] = useState(false);
  const [checked, setChecked] = useState<Set<string>>(new Set());

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(
    () => setPageNumber(1),
    [statusFilter, debtFilter, studentFilter, groupFilter, periodFrom, periodTo, sortBy],
  );

  const studentsQuery = useQuery({
    queryKey: ["students", { pageSize: 100, for: "invoices" }],
    queryFn: () => searchStudents({ pageSize: 100 }),
    staleTime: 60_000,
    enabled: canView,
  });
  const groupsQuery = useQuery({
    queryKey: ["study-groups", { pageSize: 100, for: "invoices" }],
    queryFn: () => searchStudyGroups({ pageSize: 100 }),
    staleTime: 60_000,
    enabled: canView,
  });

  const studentName = useMemo(() => {
    const m = new Map<string, string>();
    for (const s of studentsQuery.data?.items ?? []) m.set(s.id, s.displayName);
    return m;
  }, [studentsQuery.data]);
  const groupName = useMemo(() => {
    const m = new Map<string, string>();
    for (const g of groupsQuery.data?.items ?? []) m.set(g.id, g.name);
    return m;
  }, [groupsQuery.data]);

  const studentOptions = useMemo(
    () =>
      (studentsQuery.data?.items ?? []).map((s) => ({
        value: s.id,
        label: s.displayName,
      })),
    [studentsQuery.data],
  );
  const groupOptions = useMemo(
    () =>
      (groupsQuery.data?.items ?? []).map((g) => ({
        value: g.id,
        label: `${g.name} · ${g.code}`,
      })),
    [groupsQuery.data],
  );

  const params = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      status: statusFilter === "all" ? null : statusFilter,
      hasDebt: debtFilter === "debt" ? true : null,
      studentId: studentFilter,
      studyGroupId: groupFilter,
      periodFrom: periodFrom || null,
      periodTo: periodTo || null,
      sortBy,
      sortDir: sortBy === "number" ? ("asc" as const) : ("desc" as const),
    }),
    [
      pageNumber,
      debouncedSearch,
      statusFilter,
      debtFilter,
      studentFilter,
      groupFilter,
      periodFrom,
      periodTo,
      sortBy,
    ],
  );

  const query = useQuery({
    queryKey: ["student-invoices", params],
    queryFn: () => searchStudentInvoices(params),
    placeholderData: keepPreviousData,
    enabled: canView,
  });

  const data = query.data;
  const items = useMemo(() => data?.items ?? [], [data]);

  // Keep the checked set scoped to still-visible drafts.
  useEffect(() => {
    setChecked((prev) => {
      const next = new Set<string>();
      for (const it of items) {
        if (it.status === "Draft" && prev.has(it.id)) next.add(it.id);
      }
      return next.size === prev.size ? prev : next;
    });
  }, [items]);

  const bulkIssueMut = useMutation({
    mutationFn: (ids: string[]) => bulkIssueInvoices(ids, today()),
    onSuccess: (issued, ids) => {
      toast.success(
        `Выставлено счетов: ${issued.length} из ${ids.length}` +
          (issued.length < ids.length ? " (не-черновики пропущены)" : ""),
      );
      setChecked(new Set());
      void queryClient.invalidateQueries({ queryKey: ["student-invoices"] });
    },
    onError: (err) =>
      toast.error("Не удалось выставить счета", { description: describe(err) }),
  });

  const filtersActive =
    debouncedSearch.length > 0 ||
    statusFilter !== "all" ||
    debtFilter !== "all" ||
    studentFilter !== null ||
    groupFilter !== null ||
    periodFrom !== "" ||
    periodTo !== "";

  const clearFilters = () => {
    setSearch("");
    setStatusFilter("all");
    setDebtFilter("all");
    setStudentFilter(null);
    setGroupFilter(null);
    setPeriodFrom("");
    setPeriodTo("");
  };

  if (!canView) {
    return (
      <div className="space-y-6">
        <EntityPageHeader icon={Receipt} title="Счета учеников" />
        <EntityEmpty
          icon={Receipt}
          title="Нет доступа"
          body="Нужно право «Просмотр счетов учеников»."
        />
      </div>
    );
  }

  const toggle = (id: string) =>
    setChecked((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Receipt}
        title="Счета учеников"
        total={data?.totalCount ?? null}
        unit="счёт"
        description="Счета за обучение. Массовое выставление на группу — в мастере; отдельный счёт собирается на его карточке."
      >
        {canCreate && (
          <Button
            onClick={() => setWizardOpen(true)}
            className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
          >
            <Layers className="size-4" />
            Массовое выставление
          </Button>
        )}
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Поиск по номеру счёта…"
      />

      <div className="flex flex-wrap items-center gap-2">
        <EntityFilterPill<StatusFilter>
          label="Статус"
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: "Все" },
            { value: "Draft", label: "Черновики" },
            { value: "Issued", label: "Выставлены" },
            { value: "PartiallyPaid", label: "Частично" },
            { value: "Paid", label: "Оплачены" },
            { value: "Cancelled", label: "Отменены" },
          ]}
        />
        <EntityFilterPill<DebtFilter>
          label="Долг"
          value={debtFilter}
          onChange={setDebtFilter}
          options={[
            { value: "all", label: "Все" },
            { value: "debt", label: "С долгом" },
          ]}
        />
        <Combobox
          label="Ученик"
          value={studentFilter}
          onChange={setStudentFilter}
          options={studentOptions}
          variant="filter"
          searchable
          clearable
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
        <Combobox
          label="Сортировка"
          value={sortBy}
          onChange={(v) => setSortBy((v as SortKey) ?? "number")}
          options={[
            { value: "number", label: "По номеру" },
            { value: "dueDate", label: "По сроку оплаты" },
            { value: "total", label: "По сумме" },
            { value: "status", label: "По статусу" },
          ]}
          variant="filter"
        />
        <div className="flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]">
          <span>Период с</span>
          <Input
            type="date"
            aria-label="Период с"
            value={periodFrom}
            onChange={(e) => setPeriodFrom(e.target.value)}
            className="h-8 w-[9.5rem] text-[12px]"
          />
          <span>по</span>
          <Input
            type="date"
            aria-label="Период по"
            value={periodTo}
            onChange={(e) => setPeriodTo(e.target.value)}
            className="h-8 w-[9.5rem] text-[12px]"
          />
        </div>
      </div>

      {canIssue && checked.size > 0 && (
        <div className="flex items-center justify-between gap-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] px-4 py-2.5">
          <span className="text-[13px] text-[var(--color-foreground)]">
            Отмечено черновиков: <b>{checked.size}</b>
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setChecked(new Set())}
              disabled={bulkIssueMut.isPending}
            >
              Снять
            </Button>
            <Button
              size="sm"
              className="gap-1.5"
              disabled={bulkIssueMut.isPending}
              onClick={() => bulkIssueMut.mutate([...checked])}
            >
              <FileText className="size-3.5" />
              {bulkIssueMut.isPending ? "Выставление…" : "Выставить отмеченные"}
            </Button>
          </div>
        </div>
      )}

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Receipt}
          title={filtersActive ? "Ничего не найдено" : "Счетов пока нет"}
          body={
            filtersActive
              ? "Измените запрос или сбросьте фильтры."
              : "Выставьте счета на группу в мастере массового выставления."
          }
          action={
            filtersActive ? (
              <Button
                variant="outline"
                onClick={clearFilters}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                Сбросить фильтры
              </Button>
            ) : canCreate ? (
              <Button
                onClick={() => setWizardOpen(true)}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                <Layers className="mr-1.5 size-4" />
                Массовое выставление
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="space-y-2 md:hidden">
            {items.map((inv) => (
              <InvoiceMobileCard
                key={inv.id}
                invoice={inv}
                studentName={studentName.get(inv.studentId)}
              />
            ))}
          </div>

          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span />
              <span>Счёт</span>
              <span className="hidden lg:block">Период</span>
              <span className="hidden lg:block">Сумма</span>
              <span className="hidden lg:block">Оплачено</span>
              <span>Статус</span>
              <span />
            </EntityListHeader>
            {items.map((inv, i) => (
              <InvoiceDesktopRow
                key={inv.id}
                invoice={inv}
                studentName={studentName.get(inv.studentId)}
                groupName={inv.studyGroupId ? groupName.get(inv.studyGroupId) : undefined}
                isLast={i === items.length - 1}
                checkable={canIssue && inv.status === "Draft"}
                isChecked={checked.has(inv.id)}
                onToggle={() => toggle(inv.id)}
              />
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? 1}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      {wizardOpen && (
        <BulkGenerateWizard
          groupOptions={groupOptions}
          canIssue={canIssue}
          onClose={() => setWizardOpen(false)}
          onDone={() =>
            void queryClient.invalidateQueries({ queryKey: ["student-invoices"] })
          }
        />
      )}
    </div>
  );
}

function StatusPills({ invoice }: { invoice: StudentInvoiceDto }) {
  return (
    <span className="flex items-center gap-1.5">
      <EntityStatusBadge tone={INVOICE_STATUS_TONE[invoice.status]}>
        {INVOICE_STATUS_LABEL[invoice.status]}
      </EntityStatusBadge>
      {invoice.isOverdue && (
        <EntityStatusBadge tone="danger">просрочен</EntityStatusBadge>
      )}
    </span>
  );
}

function InvoiceMobileCard({
  invoice,
  studentName,
}: {
  invoice: StudentInvoiceDto;
  studentName?: string;
}) {
  return (
    <EntityMobileCard
      href={`/payments/invoices/${invoice.id}`}
      aria-label={`Открыть счёт ${invoice.number}`}
      dim={invoice.status === "Cancelled"}
    >
      <div className="flex items-center justify-between">
        <div className="min-w-0">
          <p className="truncate text-[14px] font-medium text-[var(--color-foreground)]">
            <span className="font-mono">{invoice.number}</span>
          </p>
          <p className="mt-0.5 truncate text-[11px] text-[var(--color-muted-foreground)]">
            {studentName ?? "Ученик"} · {formatDate(invoice.periodFrom)} —{" "}
            {formatDate(invoice.periodTo)}
          </p>
        </div>
        <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
      </div>
      <div className="mt-2 flex items-center justify-between gap-2">
        <StatusPills invoice={invoice} />
        <span className="tabular-nums text-[12px] text-[var(--color-foreground)]">
          {formatMoney(invoice.paidAmount, invoice.currency)} /{" "}
          {formatMoney(invoice.total, invoice.currency)}
        </span>
      </div>
    </EntityMobileCard>
  );
}

function InvoiceDesktopRow({
  invoice,
  studentName,
  groupName,
  isLast,
  checkable,
  isChecked,
  onToggle,
}: {
  invoice: StudentInvoiceDto;
  studentName?: string;
  groupName?: string;
  isLast: boolean;
  checkable: boolean;
  isChecked: boolean;
  onToggle: () => void;
}) {
  return (
    <EntityListRow
      className={DESKTOP_COLS}
      isLast={isLast}
      dim={invoice.status === "Cancelled"}
    >
      <span className="flex items-center">
        {checkable ? (
          <input
            type="checkbox"
            aria-label={`Отметить счёт ${invoice.number}`}
            checked={isChecked}
            onChange={onToggle}
          />
        ) : null}
      </span>

      <Link
        to={`/payments/invoices/${invoice.id}`}
        className="flex min-w-0 flex-col outline-none"
      >
        <span className="truncate font-mono text-[13px] font-medium text-[var(--color-foreground)] transition-colors group-hover:text-[var(--color-primary)]">
          {invoice.number}
        </span>
        <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">
          {studentName ?? "Ученик"}
          {groupName ? ` · ${groupName}` : ""}
        </span>
      </Link>

      <span className="hidden items-center text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {formatDate(invoice.periodFrom)} — {formatDate(invoice.periodTo)}
      </span>

      <span className="hidden items-center tabular-nums text-[12px] text-[var(--color-foreground)] lg:flex">
        {formatMoney(invoice.total, invoice.currency)}
      </span>

      <span className="hidden items-center tabular-nums text-[12px] text-[var(--color-muted-foreground)] lg:flex">
        {formatMoney(invoice.paidAmount, invoice.currency)}
      </span>

      <span className="flex items-center">
        <StatusPills invoice={invoice} />
      </span>

      <div className="flex items-center justify-end">
        <ChevronRight className="size-4 text-[var(--color-border)] transition-colors group-hover:text-[var(--color-muted-foreground)]" />
      </div>
    </EntityListRow>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Bulk-generate wizard — group → period → due date → generate.
//  `issueImmediately` is always false here; issuing is a separate,
//  explicit action on the result step (gated on StudentInvoices.Issue).
// ───────────────────────────────────────────────────────────────────────

type WizardStep = "group" | "period" | "review" | "result";

function BulkGenerateWizard({
  groupOptions,
  canIssue,
  onClose,
  onDone,
}: {
  groupOptions: { value: string; label: string }[];
  canIssue: boolean;
  onClose: () => void;
  onDone: () => void;
}) {
  const [step, setStep] = useState<WizardStep>("group");
  const [studyGroupId, setStudyGroupId] = useState<string | null>(null);
  const [periodFrom, setPeriodFrom] = useState("");
  const [periodTo, setPeriodTo] = useState("");
  const [dueDate, setDueDate] = useState("");
  const [resultIds, setResultIds] = useState<string[]>([]);

  const generateMut = useMutation({
    mutationFn: (input: BulkGenerateInput) => bulkGenerateInvoices(input),
    onSuccess: (ids) => {
      setResultIds(ids);
      setStep("result");
      onDone();
      toast.success(
        ids.length === 0
          ? "В группе нет активных учеников с тарифом за этот период"
          : `Готово: ${ids.length} счёт(ов) — черновики`,
      );
    },
    onError: (err) =>
      toast.error("Не удалось выставить счета", { description: describe(err) }),
  });

  const issueMut = useMutation({
    mutationFn: (ids: string[]) => bulkIssueInvoices(ids, today()),
    onSuccess: (issued) => {
      toast.success(`Выставлено сразу: ${issued.length} из ${resultIds.length}`);
      onDone();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось выставить счета", { description: describe(err) }),
  });

  const groupLabel =
    groupOptions.find((g) => g.value === studyGroupId)?.label ?? "";

  const periodValid =
    periodFrom.length > 0 && periodTo.length > 0 && periodFrom <= periodTo;
  const dueValid = dueDate.length > 0 && dueDate >= periodFrom;

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (!studyGroupId || !periodValid || !dueValid) return;
    generateMut.mutate({
      studyGroupId,
      periodFrom,
      periodTo,
      dueDate,
      issueImmediately: false,
    });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={submit}>
          <DialogHeader>
            <DialogTitle>Массовое выставление счетов</DialogTitle>
            <DialogDescription>
              Черновики создаются на активный состав группы за период. Повторный
              запуск за тот же период вернёт те же счета — дублей не будет.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <ol className="flex items-center gap-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
              <li className={step === "group" ? "text-[var(--color-primary)]" : ""}>
                1. Группа
              </li>
              <li>·</li>
              <li className={step === "period" ? "text-[var(--color-primary)]" : ""}>
                2. Период
              </li>
              <li>·</li>
              <li
                className={
                  step === "review" || step === "result"
                    ? "text-[var(--color-primary)]"
                    : ""
                }
              >
                3. Готово
              </li>
            </ol>

            {step === "group" && (
              <Field id="bw-group" label="Учебная группа" required>
                <Combobox
                  id="bw-group"
                  label="Учебная группа"
                  value={studyGroupId}
                  onChange={setStudyGroupId}
                  options={groupOptions}
                  placeholder="Выберите группу"
                  searchable
                />
              </Field>
            )}

            {step === "period" && (
              <>
                <div className="grid gap-3 sm:grid-cols-2">
                  <Field id="bw-from" label="Период с" required>
                    <Input
                      id="bw-from"
                      type="date"
                      value={periodFrom}
                      onChange={(e) => setPeriodFrom(e.target.value)}
                      required
                    />
                  </Field>
                  <Field id="bw-to" label="Период по" required>
                    <Input
                      id="bw-to"
                      type="date"
                      value={periodTo}
                      onChange={(e) => setPeriodTo(e.target.value)}
                      required
                    />
                  </Field>
                </div>
                <Field
                  id="bw-due"
                  label="Срок оплаты"
                  required
                  hint="Дата, после которой неоплаченный счёт считается просроченным."
                >
                  <Input
                    id="bw-due"
                    type="date"
                    value={dueDate}
                    onChange={(e) => setDueDate(e.target.value)}
                    required
                  />
                </Field>
                {!periodValid && periodFrom && periodTo && (
                  <p className="text-[12px] text-[var(--color-destructive)]">
                    «Период по» не может быть раньше «Период с».
                  </p>
                )}
              </>
            )}

            {step === "review" && (
              <dl className="space-y-2 rounded-lg bg-[var(--color-muted)] px-4 py-3 text-[13px]">
                <div className="flex justify-between gap-3">
                  <dt className="text-[var(--color-muted-foreground)]">Группа</dt>
                  <dd className="text-right font-medium">{groupLabel}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-[var(--color-muted-foreground)]">Период</dt>
                  <dd className="text-right font-medium">
                    {formatDate(periodFrom)} — {formatDate(periodTo)}
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-[var(--color-muted-foreground)]">Срок оплаты</dt>
                  <dd className="text-right font-medium">{formatDate(dueDate)}</dd>
                </div>
                <p className="pt-1 text-[12px] text-[var(--color-muted-foreground)]">
                  Счета создаются как <b>черновики</b>. Выставить их можно сразу
                  на следующем шаге или позже из списка.
                </p>
              </dl>
            )}

            {step === "result" && (
              <div className="space-y-3">
                <p className="text-[13px] text-[var(--color-foreground)]">
                  {resultIds.length === 0
                    ? "Ни одного счёта не создано — в группе нет активных учеников с тарифом за этот период."
                    : `Счетов (создано или уже существовало): ${resultIds.length}.`}
                </p>
                {resultIds.length > 0 && (
                  <ul className="max-h-52 space-y-1 overflow-auto rounded-lg border border-[var(--color-border)] p-2">
                    {resultIds.map((id) => (
                      <li key={id}>
                        <Link
                          to={`/payments/invoices/${id}`}
                          className="block truncate rounded px-2 py-1 font-mono text-[12px] text-[var(--color-primary)] hover:bg-[var(--color-muted)]"
                          onClick={onClose}
                        >
                          {id}
                        </Link>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}
          </DialogBody>

          <DialogFooter>
            {step === "result" ? (
              <>
                {canIssue && resultIds.length > 0 && (
                  <Button
                    type="button"
                    variant="outline"
                    disabled={issueMut.isPending}
                    onClick={() => issueMut.mutate(resultIds)}
                  >
                    {issueMut.isPending ? "…" : "Выставить все сразу"}
                  </Button>
                )}
                <Button type="button" onClick={onClose}>
                  Готово
                </Button>
              </>
            ) : (
              <>
                <DialogClose asChild>
                  <Button
                    type="button"
                    variant="outline"
                    disabled={generateMut.isPending}
                  >
                    Отмена
                  </Button>
                </DialogClose>
                {step === "group" && (
                  <Button
                    type="button"
                    disabled={!studyGroupId}
                    onClick={() => setStep("period")}
                  >
                    Далее
                  </Button>
                )}
                {step === "period" && (
                  <>
                    <Button
                      type="button"
                      variant="ghost"
                      onClick={() => setStep("group")}
                    >
                      Назад
                    </Button>
                    <Button
                      type="button"
                      disabled={!periodValid || !dueValid}
                      onClick={() => setStep("review")}
                    >
                      Далее
                    </Button>
                  </>
                )}
                {step === "review" && (
                  <>
                    <Button
                      type="button"
                      variant="ghost"
                      disabled={generateMut.isPending}
                      onClick={() => setStep("period")}
                    >
                      Назад
                    </Button>
                    <Button type="submit" disabled={generateMut.isPending}>
                      {generateMut.isPending ? "Создание…" : "Создать черновики"}
                    </Button>
                  </>
                )}
              </>
            )}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
