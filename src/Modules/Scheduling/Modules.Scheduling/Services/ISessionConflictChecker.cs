using FSH.Modules.Scheduling.Contracts.Dtos;

namespace FSH.Modules.Scheduling.Services;

/// <summary>Internal to this module — not exposed via Contracts, unlike
/// <c>IAttendanceQueryService</c>/<c>ISessionPlanQueryService</c>. See
/// docs/02 Модули/Scheduling.md → "Конфликты".</summary>
public interface ISessionConflictChecker
{
    /// <summary>Checks whether a candidate slot [<paramref name="startUtc"/>,
    /// <paramref name="endUtc"/>) clashes with an existing <c>Planned</c>/<c>Held</c> session on any
    /// of three resources — teacher, (non-virtual) room, study group.</summary>
    ValueTask<IReadOnlyList<SessionConflictDto>> CheckAsync(
        Guid? excludeSessionId,
        Guid teacherId,
        Guid? roomId,
        Guid studyGroupId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default);
}
