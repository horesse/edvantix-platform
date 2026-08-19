namespace FSH.Modules.Scheduling.Contracts;

/// <summary>Synchronous read access to planned sessions for other modules — basis of proportional
/// monthly-tariff calculation in Payments (bill for however many sessions the month actually
/// contains, not a flat count).</summary>
public interface ISessionPlanQueryService
{
    /// <summary>Count of non-cancelled (<c>Planned</c> or <c>Held</c>) sessions in
    /// [<paramref name="from"/>, <paramref name="toDate"/>] for the group.</summary>
    ValueTask<int> CountPlannedSessionsAsync(
        Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default);
}
