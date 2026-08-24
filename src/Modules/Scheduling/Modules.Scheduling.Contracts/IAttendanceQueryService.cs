using FSH.Modules.Scheduling.Contracts.Dtos;

namespace FSH.Modules.Scheduling.Contracts;

/// <summary>Synchronous read access to attendance for other modules — the basis of per-lesson
/// billing in Payments. No caching (same reasoning as StudyGroups' <c>IStudyGroupQueryService</c>) —
/// called per invoice run, not per item in a hot list.</summary>
public interface IAttendanceQueryService
{
    /// <summary>Count of <c>Held</c> sessions in [<paramref name="from"/>, <paramref name="toDate"/>]
    /// for which the student has an attendance row (i.e. was on the active roster when the session
    /// was held) — regardless of Present/Absent/Late/Excused. Payments decides per-status charging
    /// (e.g. <c>ChargeOnExcusedAbsence</c>) from <see cref="GetBreakdownAsync"/> instead.</summary>
    ValueTask<int> CountHeldSessionsAsync(
        Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate,
        CancellationToken cancellationToken = default);

    ValueTask<AttendanceBreakdown> GetBreakdownAsync(
        Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate,
        CancellationToken cancellationToken = default);
}
