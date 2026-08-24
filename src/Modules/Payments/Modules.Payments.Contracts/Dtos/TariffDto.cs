namespace FSH.Modules.Payments.Contracts.Dtos;

public sealed record TariffDto(
    Guid Id,
    string Name,
    Guid? CourseId,
    TariffKind Kind,
    decimal Amount,
    string Currency,
    int LessonsCount,
    int ValidDays,
    bool ChargeOnExcusedAbsence,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
