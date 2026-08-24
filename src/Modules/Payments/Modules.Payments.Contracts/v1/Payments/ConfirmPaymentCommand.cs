using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Payments;

public sealed record ConfirmPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string? Reference,
    Guid? ProofFileId,
    string? Note) : ICommand<Guid>;
