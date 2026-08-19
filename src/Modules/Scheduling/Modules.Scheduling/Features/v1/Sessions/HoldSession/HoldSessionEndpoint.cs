using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.HoldSession;

public static class HoldSessionEndpoint
{
    internal static RouteHandlerBuilder MapHoldSessionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/sessions/{sessionId:guid}/hold",
                async (Guid sessionId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new HoldSessionCommand(sessionId), ct);
                    return Results.NoContent();
                })
            .WithName("HoldSession")
            .WithSummary("Mark a session as held, seeding attendance")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Sessions.Update);
}
