using FSH.Framework.Core.Context;
using FSH.Modules.People.Contracts;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.GetMyStudyGroups;

/// <summary>Groups where the caller is teacher (<c>PrimaryTeacherId</c> or a roster entry) or
/// student (any enrollment, any status — the caller can see finished groups they attended). Not
/// the guardian's wards — docs/02 Модули/StudyGroups.md → «Контракты» documents this query as
/// "свои: преподаватель или ученик" only; guardians see their wards' groups via the wards'
/// own <c>GetStudentEnrollmentsQuery</c>/People profile, not this endpoint.</summary>
public sealed class GetMyStudyGroupsQueryHandler(
    StudyGroupsDbContext dbContext, IPeopleScopeResolver scopeResolver, ICurrentUser currentUser)
    : IQueryHandler<GetMyStudyGroupsQuery, IReadOnlyList<StudyGroupDto>>
{
    public async ValueTask<IReadOnlyList<StudyGroupDto>> Handle(GetMyStudyGroupsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var scope = await scopeResolver.ResolveAsync(currentUser.GetUserId().ToString(), cancellationToken)
            .ConfigureAwait(false);

        if (scope.TeacherId is null && scope.StudentId is null)
        {
            return [];
        }

        var q = dbContext.StudyGroups.AsNoTracking().AsQueryable();

        q = q.Where(g =>
            (scope.TeacherId != null && (g.PrimaryTeacherId == scope.TeacherId
                || dbContext.GroupTeachers.Any(t => t.StudyGroupId == g.Id && t.TeacherId == scope.TeacherId)))
            || (scope.StudentId != null
                && dbContext.GroupEnrollments.Any(e => e.StudyGroupId == g.Id && e.StudentId == scope.StudentId)));

        var groups = await q.OrderBy(g => g.Code).ToListAsync(cancellationToken).ConfigureAwait(false);

        var groupIds = groups.Select(g => g.Id).ToList();
        var counts = await dbContext.GroupEnrollments
            .AsNoTracking()
            .Where(e => groupIds.Contains(e.StudyGroupId)
                && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Paused))
            .GroupBy(e => e.StudyGroupId)
            .Select(g => new { StudyGroupId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var countByGroup = counts.ToDictionary(x => x.StudyGroupId, x => x.Count);

        return groups.Select(g => g.ToDto(countByGroup.GetValueOrDefault(g.Id))).ToList();
    }
}
