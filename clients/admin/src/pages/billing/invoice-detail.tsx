import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Ban, CheckCircle2, Download, FileText, Send } from "lucide-react";
import { toast } from "sonner";
import {
  downloadInvoicePdf,
  getInvoice,
  issueInvoice,
  markInvoicePaid,
  voidInvoice,
  type InvoiceStatus,
  type InvoiceLineItemDto,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Input } from "@/components/ui/input";
import { EntityPageHeader, SettingsSection, Field } from "@/components/list";
import { ApiRequestError } from "@/lib/api-client";
import { cn } from "@/lib/cn";
import { useAuth } from "@/auth/use-auth";
import { INVOICE_PURPOSE_RU, INVOICE_STATUS_RU, LINE_ITEM_KIND_RU } from "@/lib/billing-labels";
import { BillingPermissions } from "@/lib/permissions";

// ─── helpers ─────────────────────────────────────────────────────────

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

const dateLong = new Intl.DateTimeFormat("ru-RU", {
  month: "long",
  day: "numeric",
  year: "numeric",
});
function formatDate(iso?: string | null) {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : dateLong.format(d);
}

function formatPeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, "0")}`;
}

function statusVariant(status: InvoiceStatus): React.ComponentProps<typeof Badge>["variant"] {
  switch (status) {
    case "Paid":
      return "success";
    case "Issued":
      return "info";
    case "Draft":
      return "warning";
    case "Void":
      return "danger";
    default:
      return "default";
  }
}

function describe(err: unknown, fallback: string): string {
  if (err instanceof ApiRequestError) return err.problem?.detail ?? err.problem?.title ?? err.message;
  if (err instanceof Error) return err.message;
  return fallback;
}

// ─── component ───────────────────────────────────────────────────────

export function InvoiceDetailPage() {
  const { invoiceId = "" } = useParams<{ invoiceId: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { user: currentUser } = useAuth();
  // Issue / mark-paid / void and PDF download all require Billing.Manage on the server.
  const canManageBilling = (currentUser?.permissions ?? []).includes(BillingPermissions.Manage);

  const query = useQuery({
    queryKey: ["billing", "invoice", invoiceId],
    queryFn: () => getInvoice(invoiceId),
    enabled: !!invoiceId,
  });
  const invoice = query.data;

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["billing", "invoice", invoiceId] });
    queryClient.invalidateQueries({ queryKey: ["billing", "invoices"] });
  };

  // ── state-machine mutations ────────────────────────────────────────

  const [dueAt, setDueAt] = useState("");
  const [voidReason, setVoidReason] = useState("");

  // Pass id + number via mutate(arg) — never close over invoice state, which
  // could be stale if the query refetched between render and click.
  const downloadMutation = useMutation({
    mutationFn: ({ id, number }: { id: string; number: string }) => downloadInvoicePdf(id, number),
    onError: (err) => toast.error("Не удалось скачать", { description: describe(err, "Не удалось скачать PDF счёта.") }),
  });

  const issueMutation = useMutation({
    mutationFn: () => issueInvoice(invoiceId, dueAt ? new Date(dueAt).toISOString() : null),
    onSuccess: () => {
      toast.success("Счёт выставлен", { description: "Статус изменён на «Выставлен»." });
      setDueAt("");
      invalidate();
    },
    onError: (err) => toast.error("Не удалось выставить", { description: describe(err, "Не удалось выставить счёт.") }),
  });

  const payMutation = useMutation({
    mutationFn: () => markInvoicePaid(invoiceId),
    onSuccess: () => {
      toast.success("Отмечен как оплаченный");
      invalidate();
    },
    onError: (err) => toast.error("Не удалось отметить оплату", { description: describe(err, "Не удалось отметить оплату.") }),
  });

  const voidMutation = useMutation({
    mutationFn: () => voidInvoice(invoiceId, voidReason.trim() ? voidReason.trim() : null),
    onSuccess: () => {
      toast.success("Счёт аннулирован");
      setVoidReason("");
      invalidate();
    },
    onError: (err) => toast.error("Не удалось аннулировать", { description: describe(err, "Не удалось аннулировать счёт.") }),
  });

  // ── render ─────────────────────────────────────────────────────────

  return (
    <div className="space-y-6">
      <div>
        <Button variant="ghost" size="sm" onClick={() => navigate("/billing/invoices")} className="-ml-2 mb-4">
          <ArrowLeft className="mr-1 h-4 w-4" /> Все счета
        </Button>

        {query.isLoading ? (
          <div className="space-y-2">
            <Skeleton className="h-7 w-72" />
            <Skeleton className="h-4 w-96" />
          </div>
        ) : query.isError ? (
          <div className="text-sm text-[var(--color-destructive)]">
            {describe(query.error, "Не удалось загрузить счёт.")}
          </div>
        ) : invoice ? (
          <EntityPageHeader
            icon={FileText}
            tone="saffron"
            title={formatMoney(invoice.subtotalAmount, invoice.currency)}
            description={
              <span className="flex flex-wrap items-center gap-2">
                <code className="rounded bg-[var(--color-surface-2)] px-1.5 py-0.5 font-mono text-[11px] font-medium tracking-tight">
                  {invoice.invoiceNumber}
                </code>
                <Badge variant={statusVariant(invoice.status)}>{INVOICE_STATUS_RU[invoice.status] ?? invoice.status}</Badge>
                {invoice.purpose && (
                  <Badge variant="outline">
                    {INVOICE_PURPOSE_RU[invoice.purpose] ?? invoice.purpose}
                  </Badge>
                )}
                <span className="font-mono text-[11px] text-[var(--color-muted-foreground)]">
                  школа {invoice.tenantId} · период {formatPeriod(invoice.periodYear, invoice.periodMonth)} · создан {formatDate(invoice.createdAtUtc)}
                  {invoice.periodStartUtc && invoice.periodEndUtc && (
                    ` · срок ${formatDate(invoice.periodStartUtc)} – ${formatDate(invoice.periodEndUtc)}`
                  )}
                  {invoice.issuedAtUtc && ` · выставлен ${formatDate(invoice.issuedAtUtc)}`}
                  {invoice.dueAtUtc && invoice.status === "Issued" && (
                    <span className="text-[var(--color-warning)]"> · срок {formatDate(invoice.dueAtUtc)}</span>
                  )}
                  {invoice.paidAtUtc && (
                    <span className="text-[var(--color-success)]"> · оплачен {formatDate(invoice.paidAtUtc)}</span>
                  )}
                  {invoice.voidedAtUtc && (
                    <span className="text-[var(--color-destructive)]"> · аннулирован {formatDate(invoice.voidedAtUtc)}</span>
                  )}
                </span>
              </span>
            }
          >
            {canManageBilling && (
              <Button
                variant="outline"
                size="sm"
                onClick={() =>
                  downloadMutation.mutate({ id: invoice.id, number: invoice.invoiceNumber })
                }
                disabled={downloadMutation.isPending}
                title="Скачать счёт в PDF"
              >
                <Download className="mr-1.5 h-3.5 w-3.5" />
                {downloadMutation.isPending ? "Подготовка…" : "Скачать PDF"}
              </Button>
            )}
          </EntityPageHeader>
        ) : null}
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_320px]">
        {/* Line items */}
        <SettingsSection
          title="Позиции"
          description={
            invoice
              ? `позиций: ${invoice.lineItems.length}`
              : query.isError
                ? "Недоступно"
                : "Загрузка…"
          }
        >
          {query.isError ? (
            <div className="py-8 text-center text-sm text-[var(--color-destructive)]">
              {describe(query.error, "Не удалось загрузить позиции.")}
            </div>
          ) : query.isLoading ? (
            <ul className="-mx-5 divide-y divide-[var(--color-border)] border-t border-[var(--color-border)]">
              {Array.from({ length: 2 }).map((_, i) => (
                <li key={i} className="px-5 py-4">
                  <Skeleton className="h-4 w-1/2" />
                  <Skeleton className="mt-2 h-3 w-1/4" />
                </li>
              ))}
            </ul>
          ) : invoice && invoice.lineItems.length === 0 ? (
            <div className="py-8 text-center text-sm text-[var(--color-muted-foreground)]">
              Позиций нет.
            </div>
          ) : invoice ? (
            <ul className="-mx-5 border-t border-[var(--color-border)]">
              {invoice.lineItems.map((li, i) => (
                <LineItemRow key={li.id} item={li} currency={invoice.currency} delayIndex={i} />
              ))}
              <li className="grid grid-cols-[1fr_auto] items-baseline gap-x-6 border-t-2 border-[var(--color-border-strong)] px-5 py-4">
                <div className="font-mono text-[11px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
                  итого
                </div>
                <div className="text-display text-xl font-semibold tabular-nums">
                  {formatMoney(invoice.subtotalAmount, invoice.currency)}
                </div>
              </li>
            </ul>
          ) : null}
        </SettingsSection>

        {/* Actions side panel */}
        <div className="space-y-4">
          {invoice && (
            <>
              {/* Issue / Mark-paid / Void all mutate invoice state — gated behind
                  Billing.Manage. View-only users still see read-only Notes below. */}
              {canManageBilling && (
                <>
              {/* Issue */}
              <SettingsSection
                icon={Send}
                title="Выставить"
                description="Перевод из «Черновик» в «Выставлен»."
              >
                <div className={cn("space-y-3", invoice.status !== "Draft" && "opacity-60")}>
                  <Field id="dueAt" label="Срок оплаты" hint="Пусто — значение сервера по умолчанию (+14 дней).">
                    <Input
                      id="dueAt"
                      type="date"
                      value={dueAt}
                      onChange={(e) => setDueAt(e.target.value)}
                      disabled={invoice.status !== "Draft" || issueMutation.isPending}
                    />
                  </Field>
                  <Button
                    size="sm"
                    disabled={invoice.status !== "Draft" || issueMutation.isPending}
                    onClick={() => issueMutation.mutate()}
                    className="w-full"
                  >
                    {issueMutation.isPending ? "Выставление…" : "Выставить счёт"}
                  </Button>
                </div>
              </SettingsSection>

              {/* Mark paid */}
              <SettingsSection
                icon={CheckCircle2}
                title="Отметить оплату"
                description="Фиксирует ручное получение оплаты. Идемпотентно."
              >
                <div className={cn(invoice.status !== "Issued" && "opacity-60")}>
                  <Button
                    size="sm"
                    disabled={invoice.status !== "Issued" || payMutation.isPending}
                    onClick={() => payMutation.mutate()}
                    className="w-full"
                  >
                    {payMutation.isPending ? "Сохранение…" : "Отметить оплаченным"}
                  </Button>
                </div>
              </SettingsSection>

              {/* Void */}
              <SettingsSection
                icon={Ban}
                title="Аннулировать"
                description="Отмена из «Черновик» или «Выставлен». Необратимо."
              >
                <div
                  className={cn(
                    "space-y-3",
                    (invoice.status === "Paid" || invoice.status === "Void") && "opacity-60",
                  )}
                >
                  <Field id="voidReason" label="Причина" hint="Необязательно — добавится в примечания.">
                    <Input
                      id="voidReason"
                      placeholder="дубль · спор · …"
                      value={voidReason}
                      onChange={(e) => setVoidReason(e.target.value)}
                      disabled={
                        invoice.status === "Paid" ||
                        invoice.status === "Void" ||
                        voidMutation.isPending
                      }
                    />
                  </Field>
                  <Button
                    variant="destructive"
                    size="sm"
                    disabled={
                      invoice.status === "Paid" ||
                      invoice.status === "Void" ||
                      voidMutation.isPending
                    }
                    onClick={() => voidMutation.mutate()}
                    className="w-full"
                  >
                    {voidMutation.isPending ? "Аннулирование…" : "Аннулировать счёт"}
                  </Button>
                </div>
              </SettingsSection>
                </>
              )}

              {invoice.notes && (
                <SettingsSection title="Примечания">
                  <p className="whitespace-pre-line text-xs text-[var(--color-foreground)]">
                    {invoice.notes}
                  </p>
                </SettingsSection>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}

// ─── subcomponents ───────────────────────────────────────────────────

function LineItemRow({
  item,
  currency,
  delayIndex,
}: {
  item: InvoiceLineItemDto;
  currency: string;
  delayIndex: number;
}) {
  return (
    <li
      className="fsh-enter grid grid-cols-[1fr_auto] items-baseline gap-x-6 border-b border-[var(--color-border)] last:border-b-0 px-5 py-3"
      style={{ animationDelay: `${Math.min(delayIndex, 6) * 30}ms` }}
    >
      <div className="min-w-0">
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm font-medium">{item.description}</span>
          <Badge variant={item.kind === "BaseFee" ? "default" : item.kind === "Overage" ? "warning" : "muted"}>
            {LINE_ITEM_KIND_RU[item.kind] ?? item.kind}
          </Badge>
          {item.resource && (
            <span className="font-mono text-[10.5px] uppercase tracking-[0.18em] text-[var(--color-muted-foreground)]">
              {item.resource}
            </span>
          )}
        </div>
        <div className="mt-1 font-mono text-[11px] text-[var(--color-muted-foreground)] tabular-nums">
          {item.quantity.toLocaleString("ru-RU")} × {formatMoney(item.unitPrice, currency)}
        </div>
      </div>
      <div className="text-right text-sm font-semibold tabular-nums">
        {formatMoney(item.amount, currency)}
      </div>
    </li>
  );
}

