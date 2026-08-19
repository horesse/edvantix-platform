using FSH.Framework.Core.Context;
using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetMySchedule;

/// <summary>Teacher sees own sessions (<c>TeacherId</c> match); student/guardian see the sessions of
/// every group they (or their wards) have an active/paused enrollment in — resolved via
/// <c>IStudyGroupQueryService.GetActiveStudyGroupIdsForStudentAsync</c>, not a local join (no FK
/// across the module boundary).</summary>
public sealed class GetMyScheduleQueryHandler(
    SchedulingDbContext dbContext,
    IPeopleScopeResolver scopeResolver,
    ICurrentUser currentUser,
    IStudyGroupQueryService studyGroupQueryService)
    : IQueryHandler<GetMyScheduleQuery, IReadOnlyList<SessionDto>>
{
    public async ValueTask<IReadOnlyList<SessionDto>> Handle(GetMyScheduleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var scope = await scopeResolver.ResolveAsync(currentUser.GetUserId().ToString(), cancellationToken)
            .ConfigureAwait(false);

        var groupIds = new HashSet<Guid>();
        foreach (var studentId in EnumerateStudentIds(scope))
        {
            var ids = await studyGroupQueryService.GetActiveStudyGroupIdsForStudentAsync(studentId, cancellationToken)
                .ConfigureAwait(false);
            foreach (var id in ids)
            {
                groupIds.Add(id);
            }
        }

        if (scope.TeacherId is null && groupIds.Count == 0)
        {
            return [];
        }

        var sessions = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.StartUtc >= query.From && s.StartUtc <= query.To)
            .Where(s => (scope.TeacherId != null && s.TeacherId == scope.TeacherId) || groupIds.Contains(s.StudyGroupId))
            .OrderBy(s => s.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return sessions.Select(s => s.ToDto()).ToList();
    }

    private static IEnumerable<Guid> EnumerateStudentIds(PeopleScope scope)
    {
        if (scope.StudentId is { } studentId)
        {
            yield return studentId;
        }

        foreach (var wardId in scope.WardStudentIds)
        {
            yield return wardId;
        }
    }
}
