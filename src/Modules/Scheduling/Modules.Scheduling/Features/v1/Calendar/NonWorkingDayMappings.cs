using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Features.v1.Calendar;

internal static class NonWorkingDayMappings
{
    public static NonWorkingDayDto ToDto(this NonWorkingDay d) => new(d.Id, d.Date, d.Description);
}
