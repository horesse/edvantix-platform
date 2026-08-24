namespace FSH.Modules.Payments.Contracts.Dtos;

public sealed record StudentInvoiceDto(
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
    DateTimeOffset? UpdatedAtUtc);
