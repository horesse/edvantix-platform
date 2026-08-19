using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.GetNonWorkingDays;

public sealed class GetNonWorkingDaysQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetNonWorkingDaysQuery, IReadOnlyList<NonWorkingDayDto>>
{
    public async ValueTask<IReadOnlyList<NonWorkingDayDto>> Handle(
        GetNonWorkingDaysQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var days = await dbContext.NonWorkingDays
            .AsNoTracking()
            .Where(d => query.From == null || d.Date >= query.From)
            .Where(d => query.To == null || d.Date <= query.To)
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return days.Select(d => d.ToDto()).ToList();
    }
}
