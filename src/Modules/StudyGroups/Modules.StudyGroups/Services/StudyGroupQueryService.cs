using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Services;

/// <summary>
/// Read-only cross-module access to group rosters. No caching (same reasoning as Curriculum's
/// <c>CourseQueryService</c>) — Scheduling/Payments call these per-command (attendance generation,
/// invoice run), not per-item in a hot list; revisit if profiling says otherwise once those
/// modules exist.
/// </summary>
public sealed class StudyGroupQueryService(StudyGroupsDbContext dbContext) : IStudyGroupQueryService
{
    public async ValueTask<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(
        Guid studyGroupId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        // "Active as of onDate": enrolled on or before the date, not yet left as of the date (a
        // student who left on exactly onDate is not counted — attendance/accrual for that day
        // belongs to whoever is still enrolled at day's end).
        return await dbContext.GroupEnrollments
            .AsNoTracking()
            .Where(e => e.StudyGroupId == studyGroupId
                && e.Status == EnrollmentStatus.Active
                && e.EnrolledOn <= onDate
                && (e.LeftOn == null || e.LeftOn > onDate))
            .Select(e => e.StudentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<bool> IsStudentActiveInGroupAsync(
        Guid studentId, Guid studyGroupId, DateOnly onDate, CancellationToken cancellationToken = default)
    {
        return await dbContext.GroupEnrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudyGroupId == studyGroupId
                && e.StudentId == studentId
                && e.Status == EnrollmentStatus.Active
                && e.EnrolledOn <= onDate
                && (e.LeftOn == null || e.LeftOn > onDate), cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<StudyGroupBriefDto?> GetBriefAsync(Guid studyGroupId, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.StudyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == studyGroupId, cancellationToken)
            .ConfigureAwait(false);

        return group is null
            ? null
            : new StudyGroupBriefDto(group.Id, group.Code, group.Name, group.CourseId, group.PrimaryTeacherId, group.Status);
    }
}
