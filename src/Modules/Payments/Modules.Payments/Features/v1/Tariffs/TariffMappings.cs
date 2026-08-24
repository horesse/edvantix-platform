using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Domain;

namespace FSH.Modules.Payments.Features.v1.Tariffs;

internal static class TariffMappings
{
    public static TariffDto ToDto(this Tariff t) => new(
        t.Id,
        t.Name,
        t.CourseId,
        t.Kind,
        t.Amount,
        t.Currency,
        t.LessonsCount,
        t.ValidDays,
        t.ChargeOnExcusedAbsence,
        t.IsActive,
        t.CreatedAtUtc,
        t.UpdatedAtUtc);
}
