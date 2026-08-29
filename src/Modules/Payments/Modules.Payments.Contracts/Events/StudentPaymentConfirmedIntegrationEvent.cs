using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Payments.Contracts.Dtos;

namespace FSH.Modules.Payments.Contracts.Events;

public sealed record StudentPaymentConfirmedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid InvoiceId,
    Guid StudentId,
    Guid? PayerGuardianId,
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string Number,
    string Currency)
    : IIntegrationEvent;
