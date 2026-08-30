import { useMemo, useRef, useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Ban,
  Coins,
  Download,
  FileText,
  Paperclip,
  Plus,
  Receipt,
  RotateCcw,
  Trash2,
  Undo2,
  Wallet,
} from "lucide-react";
import { toast } from "sonner";
import {
  advance,
  cancelInvoice,
  confirmPayment,
  downloadInvoicePdf,
  getStudentInvoiceById,
  getTariffs,
  issueInvoice,
  lineAmount,
  outstanding,
  reversePayment,
  updateStudentInvoice,
  type ConfirmPaymentInput,
  type InvoiceLineInput,
  type PaymentConfirmationDto,
  type PaymentMethod,
  type StudentInvoiceDetailDto,
  type TariffDto,
  PAYMENT_METHODS,
} from "@/api/payments";
import { getFileDownloadUrl } from "@/api/files";
import { getStudentById } from "@/api/people";
import { useAuth } from "@/auth/use-auth";
import { useFileUpload } from "@/hooks/use-file-upload";
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
  EntityDetailBack,
  EntityDetailHero,
  EntityDetailAvatar,
  EntityDetailSection,
  EntityDetailStat,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { cn } from "@/lib/cn";
import { describe, formatDate, formatDateTimeMono } from "@/lib/list-helpers";
import {
  formatMoney,
  INVOICE_STATUS_LABEL,
  INVOICE_STATUS_TONE,
  isDraft,
  PAYMENT_METHOD_LABEL,
} from "./payments-ui";

const today = () => new Date().toISOString().slice(0, 10);

export function InvoiceDetailPage() {
  const { invoiceId = "" } = useParams();
  const queryClient = useQueryClient();
  const perms = useAuth().user?.permissions ?? [];
  const canView = perms.includes("Permissions.Payments.StudentInvoices.View");
  const canEdit = perms.includes("Permissions.Payments.StudentInvoices.Create");
  const canIssue = perms.includes("Permissions.Payments.StudentInvoices.Issue");
  const canCancel = perms.includes("Permissions.Payments.StudentInvoices.Cancel");
  const canConfirm = perms.includes("Permissions.Payments.StudentPayments.Confirm");
  const canRevoke = perms.includes("Permissions.Payments.StudentPayments.Revoke");

  const [confirmOpen, setConfirmOpen] = useState(false);
  const [reverseTarget, setReverseTarget] = useState<PaymentConfirmationDto | null>(
    null,
  );
  const [cancelOpen, setCancelOpen] = useState(false);

  const query = useQuery({
    queryKey: ["student-invoice", invoiceId],
    queryFn: () => getStudentInvoiceById(invoiceId),
    enabled: Boolean(invoiceId) && canView,
  });
  const invoice = query.data;

  const studentQuery = useQuery({
    queryKey: ["student", invoice?.studentId],
    queryFn: () => getStudentById(invoice!.studentId),
    enabled: Boolean(invoice?.studentId),
    staleTime: 60_000,
  });

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["student-invoice", invoiceId] });
    void queryClient.invalidateQueries({ queryKey: ["student-invoices"] });
  };

  const issueMut = useMutation({
    mutationFn: () => issueInvoice(invoiceId, today()),
    onSuccess: () => {
      toast.success("Счёт выставлен");
      invalidate();
    },
    onError: (err) =>
      toast.error("Не удалось выставить счёт", { description: describe(err) }),
  });

  const cancelMut = useMutation({
    mutationFn: (reason: string) => cancelInvoice(invoiceId, reason),
    onSuccess: () => {
      toast.success("Счёт отменён");
      setCancelOpen(false);
      invalidate();
    },
    onError: (err) =>
      toast.error("Не удалось отменить счёт", { description: describe(err) }),
  });

  const pdfMut = useMutation({
    mutationFn: () => downloadInvoicePdf(invoiceId, invoice?.number ?? invoiceId),
    onError: (err) =>
      toast.error("Не удалось скачать PDF", { description: describe(err) }),
  });

  if (!canView) {
    return (
      <div>
        <EntityDetailBack to="/payments/invoices" label="К списку счетов" />
        <p className="rounded-lg border border-[var(--color-border)] px-3 py-2 text-sm text-[var(--color-muted-foreground)]">
          Нужно право «Просмотр счетов учеников».
        </p>
      </div>
    );
  }

  if (query.isLoading) {
    return (
      <div>
        <EntityDetailBack to="/payments/invoices" label="К списку счетов" />
        <div className="h-40 animate-pulse rounded-xl bg-[var(--color-muted)]" />
      </div>
    );
  }

  if (query.isError || !invoice) {
    return (
      <div>
        <EntityDetailBack to="/payments/invoices" label="К списку счетов" />
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {query.error ? describe(query.error) : "Счёт не найден"}
        </div>
      </div>
    );
  }

  const due = outstanding(invoice);
  const adv = advance(invoice);
  const paidAmount = invoice.paidAmount;
  const canCancelNow = canCancel && paidAmount === 0 && invoice.status !== "Cancelled";
  const canIssueNow = canIssue && invoice.status === "Draft";

  return (
    <div>
      <EntityDetailBack to="/payments/invoices" label="К списку счетов" />

      <EntityDetailHero
        avatar={<EntityDetailAvatar name={invoice.number} icon={Receipt} />}
        title={<span className="font-mono">{invoice.number}</span>}
        badges={
          <>
            <EntityStatusBadge tone={INVOICE_STATUS_TONE[invoice.status]}>
              {INVOICE_STATUS_LABEL[invoice.status]}
            </EntityStatusBadge>
            {invoice.isOverdue && (
              <EntityStatusBadge tone="danger">просрочен</EntityStatusBadge>
            )}
          </>
        }
        subtitle={
          <>
            <Link
              to={`/students/${invoice.studentId}`}
              className="hover:underline"
            >
              {studentQuery.data?.displayName ?? "Ученик"}
            </Link>
            {" · "}
            {formatDate(invoice.periodFrom)} — {formatDate(invoice.periodTo)}
            {" · срок оплаты "}
            {formatDate(invoice.dueDate)}
          </>
        }
        actions={
          <>
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              disabled={pdfMut.isPending}
              onClick={() => pdfMut.mutate()}
            >
              <Download className="size-3.5" />
              PDF
            </Button>
            {canIssueNow && (
              <Button
                size="sm"
                className="gap-1.5"
                disabled={issueMut.isPending || invoice.lines.length === 0}
                title={
                  invoice.lines.length === 0
                    ? "Добавьте хотя бы одну строку"
                    : undefined
                }
                onClick={() => issueMut.mutate()}
              >
                <FileText className="size-3.5" />
                {issueMut.isPending ? "…" : "Выставить"}
              </Button>
            )}
            {canCancelNow && (
              <Button
                variant="ghost"
                size="sm"
                className="gap-1.5 text-[var(--color-destructive)]"
                onClick={() => setCancelOpen(true)}
              >
                <Ban className="size-3.5" />
                Отменить
              </Button>
            )}
          </>
        }
        stats={
          <>
            <EntityDetailStat
              icon={Wallet}
              value={formatMoney(invoice.total, invoice.currency)}
              label="сумма"
            />
            <EntityDetailStat
              icon={Coins}
              value={formatMoney(paidAmount, invoice.currency)}
              label="оплачено"
              tone="success"
            />
            {adv > 0 ? (
              <EntityDetailStat
                icon={Coins}
                value={formatMoney(adv, invoice.currency)}
                label="переплата"
                tone="primary"
              />
            ) : (
              <EntityDetailStat
                icon={Coins}
                value={formatMoney(due, invoice.currency)}
                label="к оплате"
                tone={due > 0 ? "warning" : "default"}
              />
            )}
          </>
        }
      />

      {canCancel && paidAmount > 0 && invoice.status !== "Cancelled" && (
        <p className="mb-4 rounded-lg bg-[oklch(from_var(--color-warning)_l_c_h_/_0.08)] px-4 py-2.5 text-[12px] text-[var(--color-warning)]">
          Счёт нельзя отменить, пока по нему есть оплаты. Сначала сторнируйте все
          оплаты в блоке ниже.
        </p>
      )}

      <div className="space-y-4">
        <LinesSection
          invoice={invoice}
          editable={isDraft(invoice) && canEdit}
          onSaved={invalidate}
        />

        <PaymentsSection
          invoice={invoice}
          canConfirm={canConfirm}
          canRevoke={canRevoke}
          onConfirmClick={() => setConfirmOpen(true)}
          onReverseClick={setReverseTarget}
        />
      </div>

      {confirmOpen && (
        <ConfirmPaymentDialog
          invoice={invoice}
          onClose={() => setConfirmOpen(false)}
          onSaved={invalidate}
        />
      )}
      {reverseTarget && (
        <ReversePaymentDialog
          payment={reverseTarget}
          currency={invoice.currency}
          onClose={() => setReverseTarget(null)}
          onSaved={invalidate}
        />
      )}
      {cancelOpen && (
        <CancelInvoiceDialog
          pending={cancelMut.isPending}
          onClose={() => setCancelOpen(false)}
          onConfirm={(reason) => cancelMut.mutate(reason)}
        />
      )}
    </div>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Lines — read-only table, or a row editor for a Draft invoice. Saving
//  sends the WHOLE line set (ReplaceLines) plus the invoice's own
//  period/due/comment (the PUT body requires them).
// ───────────────────────────────────────────────────────────────────────

type EditorLine = {
  key: string;
  description: string;
  tariffId: string | null;
  quantity: string;
  unitPrice: string;
};

let lineKeySeq = 0;
const nextKey = () => `l${(lineKeySeq += 1)}`;

function LinesSection({
  invoice,
  editable,
  onSaved,
}: {
  invoice: StudentInvoiceDetailDto;
  editable: boolean;
  onSaved: () => void;
}) {
  if (!editable) {
    return (
      <EntityDetailSection title="Строки счёта" icon={Receipt} padded={false}>
        {invoice.lines.length === 0 ? (
          <p className="px-5 py-5 text-[13px] text-[var(--color-muted-foreground)]">
            В счёте нет строк.
            {invoice.status === "Draft"
              ? " Черновик нельзя выставить без строк."
              : ""}
          </p>
        ) : (
          <table className="w-full text-[13px]">
            <thead>
              <tr className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] text-left text-[11px] uppercase tracking-wider text-[var(--color-muted-foreground)]">
                <th className="px-5 py-2 font-semibold">Описание</th>
                <th className="px-3 py-2 text-right font-semibold">Кол-во</th>
                <th className="px-3 py-2 text-right font-semibold">Цена</th>
                <th className="px-5 py-2 text-right font-semibold">Сумма</th>
              </tr>
            </thead>
            <tbody>
              {invoice.lines.map((l) => (
                <tr
                  key={l.id}
                  className="border-b border-[oklch(from_var(--color-border)_l_c_h_/_0.3)] last:border-0"
                >
                  <td className="px-5 py-2.5">{l.description}</td>
                  <td className="px-3 py-2.5 text-right tabular-nums">
                    {l.quantity}
                  </td>
                  <td className="px-3 py-2.5 text-right tabular-nums">
                    {formatMoney(l.unitPrice, invoice.currency)}
                  </td>
                  <td className="px-5 py-2.5 text-right font-medium tabular-nums">
                    {formatMoney(l.amount, invoice.currency)}
                  </td>
                </tr>
              ))}
              <tr>
                <td colSpan={3} className="px-5 py-2.5 text-right font-semibold">
                  Итого
                </td>
                <td className="px-5 py-2.5 text-right font-bold tabular-nums">
                  {formatMoney(invoice.total, invoice.currency)}
                </td>
              </tr>
            </tbody>
          </table>
        )}
      </EntityDetailSection>
    );
  }

  return <LineEditor invoice={invoice} onSaved={onSaved} />;
}

function LineEditor({
  invoice,
  onSaved,
}: {
  invoice: StudentInvoiceDetailDto;
  onSaved: () => void;
}) {
  const [lines, setLines] = useState<EditorLine[]>(() =>
    invoice.lines.map((l) => ({
      key: nextKey(),
      description: l.description,
      tariffId: l.tariffId ?? null,
      quantity: String(l.quantity),
      unitPrice: String(l.unitPrice),
    })),
  );
  const [periodFrom, setPeriodFrom] = useState(invoice.periodFrom);
  const [periodTo, setPeriodTo] = useState(invoice.periodTo);
  const [dueDate, setDueDate] = useState(invoice.dueDate);
  const [comment, setComment] = useState(invoice.comment ?? "");

  const tariffsQuery = useQuery({
    queryKey: ["tariffs", { isActive: true, for: "invoice-lines" }],
    queryFn: () => getTariffs(true),
    staleTime: 60_000,
  });
  const tariffById = useMemo(() => {
    const m = new Map<string, TariffDto>();
    for (const t of tariffsQuery.data ?? []) m.set(t.id, t);
    return m;
  }, [tariffsQuery.data]);
  const tariffOptions = useMemo(
    () => (tariffsQuery.data ?? []).map((t) => ({ value: t.id, label: t.name })),
    [tariffsQuery.data],
  );

  const saveMut = useMutation({
    mutationFn: (payload: InvoiceLineInput[]) =>
      updateStudentInvoice(invoice.id, {
        payerGuardianId: invoice.payerGuardianId ?? null,
        studyGroupId: invoice.studyGroupId ?? null,
        periodFrom,
        periodTo,
        dueDate,
        comment: comment || null,
        lines: payload,
      }),
    onSuccess: () => {
      toast.success("Строки счёта сохранены");
      onSaved();
    },
    onError: (err) =>
      toast.error("Не удалось сохранить строки", { description: describe(err) }),
  });

  const setLine = (key: string, patch: Partial<EditorLine>) =>
    setLines((prev) =>
      prev.map((l) => (l.key === key ? { ...l, ...patch } : l)),
    );

  const onPickTariff = (key: string, tariffId: string | null) => {
    const t = tariffId ? tariffById.get(tariffId) : undefined;
    setLines((prev) =>
      prev.map((l) => {
        if (l.key !== key) return l;
        const next = { ...l, tariffId };
        if (t) {
          if (!l.description.trim()) next.description = t.name;
          if (!l.unitPrice || Number(l.unitPrice) === 0)
            next.unitPrice = String(t.amount);
        }
        return next;
      }),
    );
  };

  const addLine = () =>
    setLines((prev) => [
      ...prev,
      { key: nextKey(), description: "", tariffId: null, quantity: "1", unitPrice: "0" },
    ]);
  const removeLine = (key: string) =>
    setLines((prev) => prev.filter((l) => l.key !== key));

  const parsed: InvoiceLineInput[] = lines.map((l) => ({
    description: l.description.trim(),
    tariffId: l.tariffId,
    quantity: Number(l.quantity.replace(",", ".")) || 0,
    unitPrice: Number(l.unitPrice.replace(",", ".")) || 0,
  }));
  const total = parsed.reduce((s, l) => s + lineAmount(l), 0);
  const valid =
    parsed.every((l) => l.description.length > 0 && l.quantity > 0 && l.unitPrice >= 0) &&
    periodFrom.length > 0 &&
    periodTo.length > 0 &&
    periodFrom <= periodTo &&
    dueDate.length > 0;

  const onSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!valid) return;
    saveMut.mutate(parsed);
  };

  return (
    <EntityDetailSection
      title="Строки счёта"
      icon={Receipt}
      description="Черновик — правьте строки и сохраняйте весь набор целиком."
    >
      <form onSubmit={onSubmit} className="space-y-4">
        <div className="grid gap-3 sm:grid-cols-3">
          <Field id="ie-from" label="Период с" required>
            <Input
              id="ie-from"
              type="date"
              value={periodFrom}
              onChange={(e) => setPeriodFrom(e.target.value)}
              required
            />
          </Field>
          <Field id="ie-to" label="Период по" required>
            <Input
              id="ie-to"
              type="date"
              value={periodTo}
              onChange={(e) => setPeriodTo(e.target.value)}
              required
            />
          </Field>
          <Field id="ie-due" label="Срок оплаты" required>
            <Input
              id="ie-due"
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              required
            />
          </Field>
        </div>

        <div className="space-y-2">
          {lines.length === 0 && (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Пока нет строк — добавьте хотя бы одну, чтобы счёт можно было выставить.
            </p>
          )}
          {lines.map((l, i) => (
            <div
              key={l.key}
              className="grid gap-2 rounded-lg border border-[var(--color-border)] p-3 sm:grid-cols-[1fr_130px_100px_110px_90px_32px]"
            >
              <Field id={`ln-desc-${l.key}`} label={i === 0 ? "Описание" : ""}>
                <Input
                  id={`ln-desc-${l.key}`}
                  aria-label={`Описание строки ${i + 1}`}
                  value={l.description}
                  onChange={(e) => setLine(l.key, { description: e.target.value })}
                  placeholder="Например: Занятия за сентябрь"
                />
              </Field>
              <Field id={`ln-tariff-${l.key}`} label={i === 0 ? "Тариф" : ""}>
                <Combobox
                  id={`ln-tariff-${l.key}`}
                  label={`Тариф строки ${i + 1}`}
                  value={l.tariffId}
                  onChange={(v) => onPickTariff(l.key, v)}
                  options={tariffOptions}
                  placeholder="—"
                  searchable
                  clearable
                />
              </Field>
              <Field id={`ln-qty-${l.key}`} label={i === 0 ? "Кол-во" : ""}>
                <Input
                  id={`ln-qty-${l.key}`}
                  aria-label={`Количество строки ${i + 1}`}
                  type="number"
                  min="0"
                  step="0.5"
                  value={l.quantity}
                  onChange={(e) => setLine(l.key, { quantity: e.target.value })}
                  className="tabular-nums"
                />
              </Field>
              <Field id={`ln-price-${l.key}`} label={i === 0 ? "Цена" : ""}>
                <Input
                  id={`ln-price-${l.key}`}
                  aria-label={`Цена строки ${i + 1}`}
                  type="number"
                  min="0"
                  step="0.01"
                  value={l.unitPrice}
                  onChange={(e) => setLine(l.key, { unitPrice: e.target.value })}
                  className="tabular-nums"
                />
              </Field>
              <div className="flex items-end pb-1 text-right text-[12px] font-medium tabular-nums">
                {formatMoney(lineAmount(parsed[i]), invoice.currency)}
              </div>
              <div className="flex items-end pb-0.5">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  aria-label={`Удалить строку ${i + 1}`}
                  className="text-[var(--color-destructive)]"
                  onClick={() => removeLine(l.key)}
                >
                  <Trash2 className="size-3.5" />
                </Button>
              </div>
            </div>
          ))}
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={addLine}
          >
            <Plus className="size-3.5" />
            Добавить строку
          </Button>
        </div>

        <Field id="ie-comment" label="Комментарий">
          <textarea
            id="ie-comment"
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            rows={2}
            maxLength={1000}
            className="w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-[var(--color-muted-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2"
          />
        </Field>

        <div className="flex items-center justify-between border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] pt-3">
          <span className="text-[13px] text-[var(--color-muted-foreground)]">
            Итого:{" "}
            <b className="text-[var(--color-foreground)] tabular-nums">
              {formatMoney(total, invoice.currency)}
            </b>
          </span>
          <Button type="submit" disabled={saveMut.isPending || !valid}>
            {saveMut.isPending ? "Сохранение…" : "Сохранить строки"}
          </Button>
        </div>
      </form>
    </EntityDetailSection>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Payments — list + confirm (prominent, own permission) + per-row
//  reverse (kept subtle, separate permission/role).
// ───────────────────────────────────────────────────────────────────────

function PaymentsSection({
  invoice,
  canConfirm,
  canRevoke,
  onConfirmClick,
  onReverseClick,
}: {
  invoice: StudentInvoiceDetailDto;
  canConfirm: boolean;
  canRevoke: boolean;
  onConfirmClick: () => void;
  onReverseClick: (p: PaymentConfirmationDto) => void;
}) {
  const payments = [...invoice.payments].sort((a, b) =>
    a.confirmedAtUtc.localeCompare(b.confirmedAtUtc),
  );
  const canRecordMore =
    invoice.status !== "Draft" && invoice.status !== "Cancelled";

  return (
    <EntityDetailSection
      title="Оплаты"
      icon={Coins}
      description="Ручное подтверждение оплат менеджером. Переплата допускается."
      action={
        canConfirm && canRecordMore ? (
          <Button size="sm" className="gap-1.5" onClick={onConfirmClick}>
            <Plus className="size-3.5" />
            Подтвердить оплату
          </Button>
        ) : undefined
      }
    >
      {payments.length === 0 ? (
        <p className="text-[13px] text-[var(--color-muted-foreground)]">
          {invoice.status === "Draft"
            ? "Черновик — оплаты появятся после выставления счёта."
            : "Оплат пока нет."}
        </p>
      ) : (
        <ul className="divide-y divide-[oklch(from_var(--color-border)_l_c_h_/_0.5)]">
          {payments.map((p) => (
            <PaymentRow
              key={p.id}
              payment={p}
              currency={invoice.currency}
              canRevoke={canRevoke}
              onReverseClick={() => onReverseClick(p)}
            />
          ))}
        </ul>
      )}
    </EntityDetailSection>
  );
}

function PaymentRow({
  payment,
  currency,
  canRevoke,
  onReverseClick,
}: {
  payment: PaymentConfirmationDto;
  currency: string;
  canRevoke: boolean;
  onReverseClick: () => void;
}) {
  const isReversal = payment.reversesId != null || payment.amount < 0;

  const proofMut = useMutation({
    mutationFn: () => getFileDownloadUrl(payment.proofFileId!, { inline: true }),
    onSuccess: (res) => window.open(res.url, "_blank", "noopener"),
    onError: (err) =>
      toast.error("Не удалось открыть чек", { description: describe(err) }),
  });

  return (
    <li className="flex items-start justify-between gap-3 py-3 first:pt-0 last:pb-0">
      <div className="min-w-0">
        <p
          className={cn(
            "text-[13px] font-medium tabular-nums",
            payment.amount < 0
              ? "text-[var(--color-destructive)]"
              : "text-[var(--color-foreground)]",
          )}
        >
          {formatMoney(payment.amount, currency)}
          {isReversal && (
            <EntityStatusBadge tone="danger" className="ml-2">
              сторно
            </EntityStatusBadge>
          )}
        </p>
        <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
          {formatDate(payment.paidOn)} · {PAYMENT_METHOD_LABEL[payment.method]}
          {payment.reference ? ` · ${payment.reference}` : ""}
        </p>
        {payment.note && (
          <p className="mt-0.5 text-[11.5px] text-[var(--color-muted-foreground)]">
            {payment.note}
          </p>
        )}
        <p className="mt-0.5 text-[11px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.7)]">
          {formatDateTimeMono(payment.confirmedAtUtc)}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-1">
        {payment.proofFileId && (
          <Button
            variant="ghost"
            size="sm"
            className="gap-1.5"
            disabled={proofMut.isPending}
            onClick={() => proofMut.mutate()}
            title="Открыть чек"
          >
            <Paperclip className="size-3.5" />
          </Button>
        )}
        {canRevoke && !isReversal && (
          <Button
            variant="ghost"
            size="sm"
            className="gap-1 text-[11px] text-[var(--color-muted-foreground)]"
            onClick={onReverseClick}
          >
            <Undo2 className="size-3.5" />
            Сторнировать
          </Button>
        )}
      </div>
    </li>
  );
}

// ───────────────────────────────────────────────────────────────────────
//  Dialogs
// ───────────────────────────────────────────────────────────────────────

function ConfirmPaymentDialog({
  invoice,
  onClose,
  onSaved,
}: {
  invoice: StudentInvoiceDetailDto;
  onClose: () => void;
  onSaved: () => void;
}) {
  const due = outstanding(invoice);
  const [amount, setAmount] = useState(due > 0 ? String(due) : "");
  const [paidOn, setPaidOn] = useState(today());
  const [method, setMethod] = useState<PaymentMethod>("Cash");
  const [reference, setReference] = useState("");
  const [note, setNote] = useState("");
  const [proofFileId, setProofFileId] = useState<string | null>(null);
  const [proofName, setProofName] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { upload, isUploading, reset: resetUpload } = useFileUpload({
    ownerType: "PaymentProof",
    ownerId: invoice.id,
    category: (file) =>
      /\.(jpe?g|png|webp|gif)$/i.test(file.name) ? "Image" : "Document",
  });

  const mutation = useMutation({
    mutationFn: (input: ConfirmPaymentInput) => confirmPayment(invoice.id, input),
    onSuccess: () => {
      toast.success("Оплата подтверждена");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось подтвердить оплату", { description: describe(err) }),
  });

  const amountNum = Number.parseFloat(amount.replace(",", "."));
  const valid = !Number.isNaN(amountNum) && amountNum > 0 && paidOn.length > 0;
  const overpay = valid && amountNum > due && due >= 0;

  const onPickFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;
    try {
      const asset = await upload(file);
      setProofFileId(asset.id);
      setProofName(asset.originalFileName);
    } catch (err) {
      toast.error("Не удалось загрузить чек", { description: describe(err) });
    }
  };

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    mutation.mutate({
      amount: amountNum,
      paidOn,
      method,
      reference: reference.trim() || null,
      proofFileId,
      note: note.trim() || null,
    });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Подтвердить оплату</DialogTitle>
            <DialogDescription>
              Счёт {invoice.number} · к оплате{" "}
              {formatMoney(due, invoice.currency)}. Переплата допускается — сумму
              больше остатка можно ввести.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="cp-amount" label="Сумма" required>
                <Input
                  id="cp-amount"
                  type="number"
                  min="0"
                  step="0.01"
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  required
                  autoFocus
                  className="tabular-nums"
                />
              </Field>
              <Field id="cp-date" label="Дата оплаты" required>
                <Input
                  id="cp-date"
                  type="date"
                  value={paidOn}
                  onChange={(e) => setPaidOn(e.target.value)}
                  required
                />
              </Field>
            </div>
            {overpay && (
              <p className="text-[12px] text-[var(--color-warning)]">
                Сумма больше остатка долга — разница уйдёт в переплату (аванс).
              </p>
            )}
            <Field id="cp-method" label="Способ" required>
              <Combobox
                id="cp-method"
                label="Способ"
                value={method}
                onChange={(v) => setMethod((v as PaymentMethod) ?? "Cash")}
                options={PAYMENT_METHODS.map((m) => ({
                  value: m,
                  label: PAYMENT_METHOD_LABEL[m],
                }))}
              />
            </Field>
            <Field id="cp-ref" label="Референс" hint="Номер операции, чека и т. п.">
              <Input
                id="cp-ref"
                value={reference}
                onChange={(e) => setReference(e.target.value)}
                maxLength={128}
              />
            </Field>
            <div>
              <span className="mb-1 block text-[12px] font-medium text-[var(--color-foreground)]">
                Чек / подтверждение
              </span>
              <div className="flex items-center gap-2">
                <input
                  ref={fileInputRef}
                  type="file"
                  accept=".pdf,.jpg,.jpeg,.png,.webp,.gif,.docx,.xlsx"
                  className="hidden"
                  onChange={onPickFile}
                />
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  disabled={isUploading}
                  onClick={() => fileInputRef.current?.click()}
                >
                  <Paperclip className="size-3.5" />
                  {isUploading
                    ? "Загрузка…"
                    : proofFileId
                      ? "Заменить файл"
                      : "Прикрепить файл"}
                </Button>
                {proofName && (
                  <span className="flex items-center gap-1 text-[12px] text-[var(--color-muted-foreground)]">
                    <span className="max-w-[10rem] truncate">{proofName}</span>
                    <button
                      type="button"
                      aria-label="Убрать файл"
                      className="text-[var(--color-destructive)]"
                      onClick={() => {
                        setProofFileId(null);
                        setProofName(null);
                        resetUpload();
                      }}
                    >
                      <Trash2 className="size-3" />
                    </button>
                  </span>
                )}
              </div>
            </div>
            <Field id="cp-note" label="Заметка">
              <textarea
                id="cp-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={2}
                maxLength={1000}
                className="w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-[var(--color-muted-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2"
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button
                type="button"
                variant="outline"
                disabled={mutation.isPending || isUploading}
              >
                Отмена
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || isUploading || !valid}
              className="gap-1.5"
            >
              <Coins className="size-4" />
              {mutation.isPending ? "Подтверждение…" : "Подтвердить"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function ReversePaymentDialog({
  payment,
  currency,
  onClose,
  onSaved,
}: {
  payment: PaymentConfirmationDto;
  currency: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [note, setNote] = useState("");

  const mutation = useMutation({
    mutationFn: (reason: string) => reversePayment(payment.id, reason),
    onSuccess: () => {
      toast.success("Оплата сторнирована");
      onSaved();
      onClose();
    },
    onError: (err) =>
      toast.error("Не удалось сторнировать оплату", { description: describe(err) }),
  });

  const valid = note.trim().length > 0;

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (valid) mutation.mutate(note.trim());
          }}
        >
          <DialogHeader>
            <DialogTitle>Сторнировать оплату</DialogTitle>
            <DialogDescription>
              Оплата на {formatMoney(payment.amount, currency)} от{" "}
              {formatDate(payment.paidOn)} будет отменена сторно-строкой с
              отрицательной суммой. Действие необратимо — укажите причину.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <Field id="rv-note" label="Причина сторно" required>
              <textarea
                id="rv-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={3}
                maxLength={1000}
                required
                autoFocus
                className="w-full rounded-md border border-[var(--color-input)] bg-transparent px-3 py-2 text-sm shadow-sm placeholder:text-[var(--color-muted-foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)] focus-visible:ring-offset-2"
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Отмена
              </Button>
            </DialogClose>
            <Button
              type="submit"
              variant="destructive"
              className="gap-1.5"
              disabled={mutation.isPending || !valid}
            >
              <RotateCcw className="size-4" />
              {mutation.isPending ? "Сторнирование…" : "Сторнировать"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function CancelInvoiceDialog({
  pending,
  onClose,
  onConfirm,
}: {
  pending: boolean;
  onClose: () => void;
  onConfirm: (reason: string) => void;
}) {
  const [reason, setReason] = useState("");
  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            onConfirm(reason.trim());
          }}
        >
          <DialogHeader>
            <DialogTitle>Отменить счёт?</DialogTitle>
            <DialogDescription>
              Счёт перейдёт в статус «Отменён». Отмена доступна только пока по
              счёту нет оплат.
            </DialogDescription>
          </DialogHeader>
          <DialogBody>
            <Field id="ci-reason" label="Причина">
              <Input
                id="ci-reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                autoFocus
              />
            </Field>
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={pending}>
                Не отменять
              </Button>
            </DialogClose>
            <Button type="submit" variant="destructive" disabled={pending}>
              {pending ? "…" : "Отменить счёт"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
