using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace FSH.Modules.StudyGroups.Contracts;

/// <summary>
/// Synchronous read access to group rosters for other modules. No caching (same reasoning as
/// Curriculum's <c>ICourseQueryService</c>) — Scheduling/Payments call these per-command
/// (attendance generation, invoice run), not per-item in a hot list.
/// </summary>
public interface IStudyGroupQueryService
{
    /// <summary>Student ids with an <see cref="EnrollmentStatus.Active"/> enrollment in the group
    /// as of <paramref name="onDate"/> — Scheduling seeds attendance rows from this,
    /// Payments computes accruals from it.</summary>
    ValueTask<IReadOnlyList<Guid>> GetActiveStudentIdsAsync(
        Guid studyGroupId, DateOnly onDate, CancellationToken cancellationToken = default);

    ValueTask<bool> IsStudentActiveInGroupAsync(
        Guid studentId, Guid studyGroupId, DateOnly onDate, CancellationToken cancellationToken = default);

    ValueTask<StudyGroupBriefDto?> GetBriefAsync(Guid studyGroupId, CancellationToken cancellationToken = default);
}
