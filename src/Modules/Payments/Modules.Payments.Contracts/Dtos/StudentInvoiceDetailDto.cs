namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary><see cref="StudentInvoiceDto"/> plus <see cref="Lines"/> and <see cref="Payments"/> —
/// "строки и оплаты" (see docs/02 Модули/Payments.md → «Контракты»).</summary>
public sealed record StudentInvoiceDetailDto(
    Guid Id,
    string Number,
    Guid StudentId,
    Guid? PayerGuardianId,
    Guid? StudyGroupId,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    decimal Total,
    decimal PaidAmount,
    string Currency,
    InvoiceStatus Status,
    DateOnly? IssuedOn,
    DateOnly DueDate,
    bool IsOverdue,
    string? Comment,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    IReadOnlyList<InvoiceLineDto> Lines,
    IReadOnlyList<PaymentConfirmationDto> Payments);
