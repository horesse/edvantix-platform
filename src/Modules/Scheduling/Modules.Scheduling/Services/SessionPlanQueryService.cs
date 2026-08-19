using FSH.Modules.Scheduling.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

public sealed class SessionPlanQueryService(SchedulingDbContext dbContext) : ISessionPlanQueryService
{
    public async ValueTask<int> CountPlannedSessionsAsync(
        Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        return await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.StudyGroupId == studyGroupId
                && s.StartUtc >= fromUtc && s.StartUtc <= toUtc
                && (s.Status == SessionStatus.Planned || s.Status == SessionStatus.Held))
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
