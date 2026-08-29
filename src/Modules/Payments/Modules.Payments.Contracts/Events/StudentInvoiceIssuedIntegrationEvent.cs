using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Payments.Contracts.Events;

public sealed record StudentInvoiceIssuedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    Guid StudentId,
    Guid? PayerGuardianId,
    decimal Total,
    DateOnly DueDate,
    string Number,
    string Currency)
    : IIntegrationEvent;
