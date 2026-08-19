using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CancelSession;

public static class CancelSessionEndpoint
{
    internal static RouteHandlerBuilder MapCancelSessionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/sessions/{sessionId:guid}/cancel",
                async (Guid sessionId, CancelSessionBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new CancelSessionCommand(sessionId, body.Reason), ct);
                    return Results.NoContent();
                })
            .WithName("CancelSession")
            .WithSummary("Cancel a session")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Sessions.Cancel);

    public sealed record CancelSessionBody(string? Reason);
}
