using FSH.Framework.Core.Context;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Services;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetMySchedule;

/// <summary>Thin wrapper over <see cref="IMyScheduleReader"/> — resolves "me" from the JWT and maps
/// the shared selection to <see cref="SessionDto"/>. The same reader backs the anonymous iCal feed
/// (<c>GET /my/schedule.ics</c>).</summary>
public sealed class GetMyScheduleQueryHandler(
    IMyScheduleReader reader,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyScheduleQuery, IReadOnlyList<SessionDto>>
{
    public async ValueTask<IReadOnlyList<SessionDto>> Handle(GetMyScheduleQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var sessions = await reader
            .ReadAsync(currentUser.GetUserId().ToString(), query.From, query.To, cancellationToken)
            .ConfigureAwait(false);

        return sessions.Select(s => s.ToDto()).ToList();
    }
}
