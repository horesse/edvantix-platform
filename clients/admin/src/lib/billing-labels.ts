/** Russian labels for Billing enums (platform invoices / subscriptions). */

export const INVOICE_STATUS_RU: Record<string, string> = {
  Draft: "Черновик",
  Issued: "Выставлен",
  Paid: "Оплачен",
  Void: "Аннулирован",
};

export const INVOICE_PURPOSE_RU: Record<string, string> = {
  Subscription: "Подписка",
  Usage: "Использование",
};

export const SUBSCRIPTION_STATUS_RU: Record<string, string> = {
  Active: "Активна",
  Suspended: "Приостановлена",
  Cancelled: "Отменена",
};

export const LINE_ITEM_KIND_RU: Record<string, string> = {
  BaseFee: "Базовая плата",
  Overage: "Превышение",
  Adjustment: "Корректировка",
};
