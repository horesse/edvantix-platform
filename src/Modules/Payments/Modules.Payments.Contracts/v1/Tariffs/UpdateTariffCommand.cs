using Mediator;

namespace FSH.Modules.Payments.Contracts.v1.Tariffs;

/// <summary><c>Kind</c>/<c>Currency</c> are not editable — see <c>Tariff.Update</c>.</summary>
public sealed record UpdateTariffCommand(
    Guid TariffId,
    string Name,
    Guid? CourseId,
    decimal Amount,
    int LessonsCount,
    int ValidDays,
    bool ChargeOnExcusedAbsence) : ICommand<Unit>;
