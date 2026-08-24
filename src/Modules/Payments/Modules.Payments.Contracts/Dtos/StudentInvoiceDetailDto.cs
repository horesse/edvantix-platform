namespace FSH.Modules.Payments.Contracts.Dtos;

/// <summary><see cref="StudentInvoiceDto"/> plus <see cref="Lines"/> — payments are added to this
/// shape once the payments feature lands (see docs/04 Задачи/Задачи · Новые модули.md → Payments →
/// шаг 8).</summary>
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
    IReadOnlyList<InvoiceLineDto> Lines);
