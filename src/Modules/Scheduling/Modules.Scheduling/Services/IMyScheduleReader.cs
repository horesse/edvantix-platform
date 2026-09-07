using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Services;

/// <summary>Shared "my schedule" selection behind both <c>GetMyScheduleQuery</c> (JWT caller) and
/// the anonymous iCal feed (<c>GET /my/schedule.ics</c>, caller resolved from the subscription
/// token). Teacher → own sessions (<c>TeacherId</c> match); student/guardian → the sessions of
/// every group they (or their wards) have an active/paused enrollment in, resolved via
/// <c>IStudyGroupQueryService</c> rather than a cross-module join.</summary>
public interface IMyScheduleReader
{
    Task<IReadOnlyList<Session>> ReadAsync(
        string userId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
