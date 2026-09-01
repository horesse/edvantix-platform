import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/lib/api-types";

export type WebhookSubscriptionDto = {
  id: string;
  url: string;
  events: string[];
  isActive: boolean;
  createdAtUtc: string;
};

export type WebhookDeliveryDto = {
  id: string;
  subscriptionId: string;
  eventType: string;
  httpStatusCode: number;
  success: boolean;
  attemptCount: number;
  attemptedAtUtc: string;
  errorMessage?: string | null;
};

export type CreateWebhookSubscriptionInput = {
  url: string;
  events: string[];
  secret?: string;
};

const ROOT = "/api/v1/webhooks";

export function listWebhookSubscriptions(
  pageNumber = 1,
  pageSize = 50,
): Promise<PagedResponse<WebhookSubscriptionDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<WebhookSubscriptionDto>>(`${ROOT}/subscriptions?${q.toString()}`);
}

export function createWebhookSubscription(input: CreateWebhookSubscriptionInput): Promise<string> {
  return apiFetch<string>(`${ROOT}/subscriptions`, {
    method: "POST",
    body: JSON.stringify({
      url: input.url,
      events: input.events,
      secret: input.secret?.trim() ? input.secret : null,
    }),
  });
}

export function deleteWebhookSubscription(id: string): Promise<void> {
  return apiFetch<void>(`${ROOT}/subscriptions/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export function testWebhookSubscription(id: string): Promise<{ success: boolean }> {
  return apiFetch<{ success: boolean }>(
    `${ROOT}/subscriptions/${encodeURIComponent(id)}/test`,
    { method: "POST" },
  );
}

// ─── event catalog ──────────────────────────────────────────────────
//
// GET /webhooks/event-types → the canonical list of integration events a
// school can relay outward. Static reference data (same for every tenant),
// gated by Webhooks.View. `name` is the selector stored on the
// subscription and echoed back in the X-Webhook-Event header; "*"
// subscribes to everything, catalogued or not.

export type WebhookEventTypeDto = {
  name: string;
  module: string;
  description: string;
};

export const WEBHOOK_WILDCARD = "*";

export function listWebhookEventCatalog(): Promise<WebhookEventTypeDto[]> {
  return apiFetch<WebhookEventTypeDto[]>(`${ROOT}/event-types`);
}

export function listWebhookDeliveries(
  subscriptionId: string,
  pageNumber = 1,
  pageSize = 50,
): Promise<PagedResponse<WebhookDeliveryDto>> {
  const q = new URLSearchParams({
    pageNumber: String(pageNumber),
    pageSize: String(pageSize),
  });
  return apiFetch<PagedResponse<WebhookDeliveryDto>>(
    `${ROOT}/subscriptions/${encodeURIComponent(subscriptionId)}/deliveries?${q.toString()}`,
  );
}

/**
 * Offline fallback for the event picker — mirrors `WebhookEventCatalog.All`
 * on the backend. Used only when `listWebhookEventCatalog()` fails; the live
 * catalog is the source of truth and is grouped by module in the UI.
 */
export const SUGGESTED_EVENT_TYPES: readonly WebhookEventTypeDto[] = [
  { name: "StudentCreatedIntegrationEvent", module: "People", description: "Создан профиль ученика." },
  { name: "StudentStatusChangedIntegrationEvent", module: "People", description: "Изменился статус ученика." },
  { name: "StudentArchivedIntegrationEvent", module: "People", description: "Ученик архивирован." },
  { name: "TeacherDeactivatedIntegrationEvent", module: "People", description: "Профиль преподавателя деактивирован." },
  { name: "GuardianLinkedToStudentIntegrationEvent", module: "People", description: "Представитель привязан к ученику." },
  { name: "CoursePublishedIntegrationEvent", module: "Curriculum", description: "Курс опубликован." },
  { name: "CourseArchivedIntegrationEvent", module: "Curriculum", description: "Курс архивирован." },
  { name: "LessonMaterialAddedIntegrationEvent", module: "Curriculum", description: "К уроку добавлен материал." },
  { name: "StudyGroupCreatedIntegrationEvent", module: "StudyGroups", description: "Создана учебная группа." },
  { name: "StudyGroupActivatedIntegrationEvent", module: "StudyGroups", description: "Учебная группа активирована." },
  { name: "StudyGroupFinishedIntegrationEvent", module: "StudyGroups", description: "Учебная группа завершила программу." },
  { name: "StudentEnrolledIntegrationEvent", module: "StudyGroups", description: "Ученик зачислен в группу." },
  { name: "StudentUnenrolledIntegrationEvent", module: "StudyGroups", description: "Ученик покинул группу." },
  { name: "SessionScheduledIntegrationEvent", module: "Scheduling", description: "Занятие поставлено в расписание." },
  { name: "SessionCancelledIntegrationEvent", module: "Scheduling", description: "Занятие отменено." },
  { name: "SessionRescheduledIntegrationEvent", module: "Scheduling", description: "Занятие перенесено." },
  { name: "SessionHeldIntegrationEvent", module: "Scheduling", description: "Занятие проведено." },
  { name: "SessionReminderDueIntegrationEvent", module: "Scheduling", description: "Занятие начнётся примерно через сутки." },
  { name: "AttendanceMarkedIntegrationEvent", module: "Scheduling", description: "Отмечена посещаемость ученика." },
  { name: "StudentInvoiceIssuedIntegrationEvent", module: "Payments", description: "Счёт ученику выставлен." },
  { name: "StudentInvoiceOverdueIntegrationEvent", module: "Payments", description: "Выставленный счёт просрочен." },
  { name: "StudentInvoiceDueSoonIntegrationEvent", module: "Payments", description: "Приближается срок оплаты счёта." },
  { name: "StudentInvoiceCancelledIntegrationEvent", module: "Payments", description: "Счёт отменён." },
  { name: "StudentPaymentConfirmedIntegrationEvent", module: "Payments", description: "Оплата по счёту подтверждена." },
];
