using FSH.Modules.Payments.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Tariffs;

public sealed record CreateTariffCommand(
    string Name,
    Guid? CourseId,
    TariffKind Kind,
    decimal Amount,
    string Currency,
    int LessonsCount,
    int ValidDays,
    bool ChargeOnExcusedAbsence) : ICommand<Guid>;
