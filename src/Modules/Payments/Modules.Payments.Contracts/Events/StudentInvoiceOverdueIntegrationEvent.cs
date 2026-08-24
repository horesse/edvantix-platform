using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Payments.Contracts.Events;

/// <summary>Published by <c>DetectOverdueInvoicesJob</c> — see
/// docs/04 Задачи/Задачи · Новые модули.md → Payments → шаг 13.</summary>
public sealed record StudentInvoiceOverdueIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    Guid StudentId,
    decimal Debt,
    int DaysOverdue)
    : IIntegrationEvent;
