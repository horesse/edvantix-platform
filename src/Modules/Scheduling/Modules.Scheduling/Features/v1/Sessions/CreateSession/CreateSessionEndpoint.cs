using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CreateSession;

public static class CreateSessionEndpoint
{
    internal static RouteHandlerBuilder MapCreateSessionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/sessions",
                async (CreateSessionCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateSession")
            .WithSummary("Create a session manually")
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(SchedulingPermissions.Sessions.Create)
            .WithIdempotency();
}
