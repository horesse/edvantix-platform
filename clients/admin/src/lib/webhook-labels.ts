/**
 * Human labels for the publishing modules in the webhook event catalog
 * (`GET /webhooks/event-types` → `WebhookEventType.module`). Uncatalogued
 * module names fall through to the raw value.
 */
export const MODULE_LABEL: Record<string, string> = {
  People: "Люди",
  Curriculum: "Учебные программы",
  StudyGroups: "Учебные группы",
  Scheduling: "Расписание",
  Payments: "Платежи",
  Billing: "Биллинг",
  Multitenancy: "Мультитенантность",
  Identity: "Идентификация",
};
