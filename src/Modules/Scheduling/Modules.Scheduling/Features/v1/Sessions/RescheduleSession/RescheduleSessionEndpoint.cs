using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.RescheduleSession;

public static class RescheduleSessionEndpoint
{
    internal static RouteHandlerBuilder MapRescheduleSessionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/sessions/{sessionId:guid}/reschedule",
                async (Guid sessionId, RescheduleSessionBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new RescheduleSessionCommand(
                            sessionId, body.NewStartUtc, body.NewEndUtc, body.RoomId, body.TeacherId, body.Force),
                        ct)))
            .WithName("RescheduleSession")
            .WithSummary("Reschedule a session to a new time")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(SchedulingPermissions.Sessions.Reschedule);

    public sealed record RescheduleSessionBody(
        DateTimeOffset NewStartUtc,
        DateTimeOffset NewEndUtc,
        Guid? RoomId,
        Guid? TeacherId,
        bool Force);
}
