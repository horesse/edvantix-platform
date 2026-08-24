namespace FSH.Modules.Payments.Contracts.Dtos;

public sealed record PaymentConfirmationDto(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    DateOnly PaidOn,
    PaymentMethod Method,
    string? Reference,
    Guid? ProofFileId,
    string ConfirmedByUserId,
    DateTimeOffset ConfirmedAtUtc,
    Guid? ReversesId,
    string? Note);
