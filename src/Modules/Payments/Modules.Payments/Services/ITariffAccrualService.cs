using FSH.Modules.Payments.Domain;
using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace FSH.Modules.Payments.Services;

/// <summary>One priced line an accrual produced for a single student/tariff/period — <c>null</c>
/// from <see cref="ITariffAccrualService.CalculateAsync"/> means "nothing to charge this period"
/// (e.g. zero chargeable sessions), not an error.</summary>
public sealed record AccrualLine(string Description, decimal Quantity, decimal UnitPrice);

/// <summary>
/// Turns a <see cref="Tariff"/> + enrollment window into one <see cref="AccrualLine"/>, per
/// <see cref="Contracts.Dtos.TariffKind"/> — see docs/02 Модули/Payments.md → «Модель начисления».
/// </summary>
public interface ITariffAccrualService
{
    ValueTask<AccrualLine?> CalculateAsync(
        Tariff tariff,
        GroupEnrollmentAccrualDto enrollment,
        Guid studyGroupId,
        DateOnly periodFrom,
        DateOnly periodTo,
        CancellationToken cancellationToken = default);
}
