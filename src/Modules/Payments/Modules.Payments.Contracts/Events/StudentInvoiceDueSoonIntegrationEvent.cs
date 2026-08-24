using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Payments.Contracts.Events;

/// <summary>Published by <c>PaymentReminderJob</c> a fixed number of days before <c>DueDate</c> — not
/// part of the original four events listed in docs/02 Модули/Payments.md → «Публикуемые события»
/// (that table only names Issued/Confirmed/Cancelled/Overdue). Added because the job table right
/// below it ("PaymentReminderJob — напоминания за N дней до DueDate") has no event to carry that
/// reminder otherwise; documented as an addition in the module doc's final update.</summary>
public sealed record StudentInvoiceDueSoonIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    Guid StudentId,
    Guid? PayerGuardianId,
    decimal Debt,
    int DaysUntilDue)
    : IIntegrationEvent;
