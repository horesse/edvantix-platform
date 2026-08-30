import type { EntityStatusTone } from "@/components/list";
import type {
  InvoiceStatus,
  PaymentMethod,
  StudentInvoiceDto,
  TariffKind,
} from "@/api/payments";

// Shared display maps for the Payments screens. Component-free module so
// importing it doesn't trip react-refresh (same pattern as scheduling-ui.ts).

export const TARIFF_KIND_LABEL: Record<TariffKind, string> = {
  PerLesson: "За занятие",
  PerMonth: "За месяц",
  PerPackage: "Пакет занятий",
  OneTime: "Разовый",
};

export const TARIFF_KIND_HINT: Record<TariffKind, string> = {
  PerLesson: "Начисляется за каждое проведённое занятие.",
  PerMonth: "Фиксированная сумма за календарный месяц.",
  PerPackage: "Оплата вперёд за пакет из N занятий на срок действия.",
  OneTime: "Единовременный платёж (вступительный взнос и т. п.).",
};

export const INVOICE_STATUS_LABEL: Record<InvoiceStatus, string> = {
  Draft: "Черновик",
  Issued: "Выставлен",
  PartiallyPaid: "Частично оплачен",
  Paid: "Оплачен",
  Cancelled: "Отменён",
};

export const INVOICE_STATUS_TONE: Record<InvoiceStatus, EntityStatusTone> = {
  Draft: "default",
  Issued: "info",
  PartiallyPaid: "warning",
  Paid: "success",
  Cancelled: "danger",
};

export const PAYMENT_METHOD_LABEL: Record<PaymentMethod, string> = {
  Cash: "Наличные",
  BankTransfer: "Банковский перевод",
  Card: "Карта",
  Online: "Онлайн",
  Other: "Другое",
};

/** A draft invoice is the only editable state — the line editor and the
 *  "issue" action gate on this; the server 409s otherwise. */
export function isDraft(inv: Pick<StudentInvoiceDto, "status">): boolean {
  return inv.status === "Draft";
}

/** Terminal — no lifecycle action applies. */
export function isClosed(inv: Pick<StudentInvoiceDto, "status">): boolean {
  return inv.status === "Paid" || inv.status === "Cancelled";
}

const currencyFmt = new Map<string, Intl.NumberFormat>();

/** Money with the invoice/tariff currency. Falls back to a plain
 *  "1234.00 XYZ" when the currency code is unknown to Intl. */
export function formatMoney(amount: number, currency: string): string {
  const code = (currency || "").toUpperCase() || "USD";
  let fmt = currencyFmt.get(code);
  if (!fmt) {
    try {
      fmt = new Intl.NumberFormat("ru-RU", { style: "currency", currency: code });
    } catch {
      return `${amount.toFixed(2)} ${code}`;
    }
    currencyFmt.set(code, fmt);
  }
  return fmt.format(amount);
}
