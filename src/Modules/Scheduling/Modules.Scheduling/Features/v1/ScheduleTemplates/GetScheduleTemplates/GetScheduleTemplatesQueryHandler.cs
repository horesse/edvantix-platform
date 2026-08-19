using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.GetScheduleTemplates;

public sealed class GetScheduleTemplatesQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetScheduleTemplatesQuery, IReadOnlyList<ScheduleTemplateDto>>
{
    public async ValueTask<IReadOnlyList<ScheduleTemplateDto>> Handle(
        GetScheduleTemplatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var templates = await dbContext.ScheduleTemplates
            .AsNoTracking()
            .Where(t => t.StudyGroupId == query.StudyGroupId)
            .OrderBy(t => t.DayOfWeek).ThenBy(t => t.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return templates.Select(t => t.ToDto()).ToList();
    }
}
