using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Payments.Contracts.Events;

public sealed record StudentInvoiceCancelledIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    string? Reason)
    : IIntegrationEvent;
