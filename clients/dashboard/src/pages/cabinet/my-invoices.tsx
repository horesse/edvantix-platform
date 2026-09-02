import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Receipt } from "lucide-react";
import {
  getMyInvoices,
  outstanding,
  type InvoiceStatus,
} from "@/api/payments";
import { useAuth } from "@/auth/use-auth";
import { WardSwitcher } from "@/cabinet/ward-switcher";
import { useWard } from "@/cabinet/use-ward";
import {
  EntityEmpty,
  EntityFilterPill,
  EntityListCard,
  EntityListHeader,
  EntityListRow,
  EntityStatusBadge,
  ErrorBand,
  PageHero,
} from "@/components/list";
import { MaterialsDebtNotice } from "@/components/materials-debt-notice";
import { describe, formatDate } from "@/lib/list-helpers";
import {
  formatMoney,
  INVOICE_STATUS_LABEL,
  INVOICE_STATUS_TONE,
} from "@/pages/payments/payments-ui";

const DESKTOP_COLS = "grid-cols-[1fr_140px_150px_120px_120px]";
const DESKTOP_COLS_NO_WARD = "grid-cols-[1fr_150px_120px_120px]";
type StatusFilter = InvoiceStatus | "all";

export function CabinetInvoicesPage() {
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Payments.StudentInvoices.ViewOwn");
  const [status, setStatus] = useState<StatusFilter>("all");
  const { wards, hasWards, selectedWardId } = useWard();

  const query = useQuery({
    queryKey: ["my-invoices", { status }],
    queryFn: () => getMyInvoices(status === "all" ? null : status),
    enabled: canView,
  });

  const wardName = useMemo(() => {
    const m = new Map<string, string>();
    for (const w of wards) m.set(w.id, w.name);
    return m;
  }, [wards]);

  const rows = useMemo(() => {
    let list = [...(query.data ?? [])];
    // Сервер уже отдаёт счета всех подопечных представителя — сужаем на
    // клиенте, когда в переключателе выбран конкретный подопечный.
    if (selectedWardId) list = list.filter((inv) => inv.studentId === selectedWardId);
    return list.sort((a, b) => b.periodFrom.localeCompare(a.periodFrom));
  }, [query.data, selectedWardId]);

  if (!canView) {
    return (
      <div className="space-y-6">
        <PageHero eyebrow="Кабинет" title="Мои счета" />
        <EntityEmpty
          icon={Receipt}
          title="Нет доступа"
          body="Нужно право «Просмотр своих счетов»."
        />
      </div>
    );
  }

  const cols = hasWards ? DESKTOP_COLS : DESKTOP_COLS_NO_WARD;

  return (
    <div className="space-y-4">
      <PageHero
        eyebrow="Кабинет"
        title="Мои счета"
        subtitle="Счета за обучение — ваши и ваших подопечных."
      />

      <WardSwitcher />

      <MaterialsDebtNotice />

      <EntityFilterPill<StatusFilter>
        label="Статус"
        value={status}
        onChange={setStatus}
        options={[
          { value: "all", label: "Все" },
          { value: "Issued", label: "Выставлены" },
          { value: "PartiallyPaid", label: "Частично" },
          { value: "Paid", label: "Оплачены" },
          { value: "Cancelled", label: "Отменены" },
        ]}
      />

      {query.isError && <ErrorBand message={describe(query.error)} />}

      {query.isLoading ? (
        <p className="text-sm text-[var(--color-muted-foreground)]">Загрузка…</p>
      ) : rows.length === 0 ? (
        <EntityEmpty icon={Receipt} title="Счетов нет" body="Пока ничего не выставлено." />
      ) : (
        <EntityListCard>
          <EntityListHeader className={cols}>
            <span>Счёт</span>
            {hasWards && <span>Ученик</span>}
            <span>Период</span>
            <span className="text-right">К оплате</span>
            <span>Статус</span>
          </EntityListHeader>
          {rows.map((inv, i) => (
            <EntityListRow
              key={inv.id}
              className={cols}
              isLast={i === rows.length - 1}
              dim={inv.status === "Cancelled"}
            >
              <span className="truncate font-mono text-[13px] text-[var(--color-foreground)]">
                {inv.number}
              </span>
              {hasWards && (
                <span className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                  {wardName.get(inv.studentId) ?? "—"}
                </span>
              )}
              <span className="text-[12px] text-[var(--color-muted-foreground)]">
                {formatDate(inv.periodFrom)} — {formatDate(inv.periodTo)}
              </span>
              <span className="text-right tabular-nums text-[13px] text-[var(--color-foreground)]">
                {formatMoney(outstanding(inv), inv.currency)}
              </span>
              <span className="flex items-center gap-1.5">
                <EntityStatusBadge tone={INVOICE_STATUS_TONE[inv.status]}>
                  {INVOICE_STATUS_LABEL[inv.status]}
                </EntityStatusBadge>
                {inv.isOverdue && (
                  <EntityStatusBadge tone="danger">просрочен</EntityStatusBadge>
                )}
              </span>
            </EntityListRow>
          ))}
        </EntityListCard>
      )}
    </div>
  );
}
