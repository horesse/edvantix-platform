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

    /// <summary>Study group ids where the student has an <see cref="EnrollmentStatus.Active"/> or
    /// <see cref="EnrollmentStatus.Paused"/> enrollment "as of now" — used by Scheduling's
    /// <c>GetMyScheduleQuery</c> to resolve a student's/guardian's own schedule without exposing
    /// <c>GroupEnrollment</c> rows across the module boundary.</summary>
    ValueTask<IReadOnlyList<Guid>> GetActiveStudyGroupIdsForStudentAsync(
        Guid studentId, CancellationToken cancellationToken = default);

    /// <summary>Same "active as of <paramref name="onDate"/>" roster as
    /// <see cref="GetActiveStudentIdsAsync"/>, but carrying each enrollment's per-student tariff
    /// override, discount and enrollment window — what Payments' bulk invoice generation needs to
    /// resolve "<c>GroupEnrollment.TariffId</c>, иначе тариф курса" and prorate a partial month.</summary>
    ValueTask<IReadOnlyList<GroupEnrollmentAccrualDto>> GetActiveEnrollmentsWithTariffAsync(
        Guid studyGroupId, DateOnly onDate, CancellationToken cancellationToken = default);
}
