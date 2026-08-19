using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetSessionById;

public static class GetSessionByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetSessionByIdEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/sessions/{sessionId:guid}",
                async (Guid sessionId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetSessionByIdQuery(sessionId), ct)))
            .WithName("GetSessionById")
            .WithSummary("Get a session by id")
            .Produces<SessionDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Sessions.View);
}
