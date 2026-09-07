using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.StudyGroups.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

internal sealed class MyScheduleReader(
    SchedulingDbContext dbContext,
    IPeopleScopeResolver scopeResolver,
    IStudyGroupQueryService studyGroupQueryService)
    : IMyScheduleReader
{
    public async Task<IReadOnlyList<Session>> ReadAsync(
        string userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        var scope = await scopeResolver.ResolveAsync(userId, cancellationToken).ConfigureAwait(false);

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

        return await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.StartUtc >= fromUtc && s.StartUtc <= toUtc)
            .Where(s => (scope.TeacherId != null && s.TeacherId == scope.TeacherId) || groupIds.Contains(s.StudyGroupId))
            .OrderBy(s => s.StartUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
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
