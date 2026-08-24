using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Scheduling.Contracts.Events;

/// <summary>Published by <c>SessionReminderJob</c> for a session starting in ~24 hours — not one of
/// the five events documented in docs/02 Модули/Scheduling.md → "Публикуемые события" (those cover
/// lifecycle transitions), but the vehicle for "напоминания за 24 часа" in the same doc's
/// «Задания Hangfire» table. Notifications does not yet subscribe to Scheduling events (see
/// docs/04 Задачи/Задачи · Доработки каркаса.md → Notifications) — actually sending the reminder is
/// out of this module's boundary; this event only makes the data available.</summary>
public sealed record SessionReminderDueIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SessionId,
    Guid StudyGroupId,
    DateTimeOffset StartUtc)
    : IIntegrationEvent;
