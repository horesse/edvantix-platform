using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.CreateRoom;

public static class CreateRoomEndpoint
{
    internal static RouteHandlerBuilder MapCreateRoomEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/rooms",
                async (CreateRoomCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateRoom")
            .WithSummary("Create a room")
            .RequirePermission(SchedulingPermissions.Rooms.Manage)
            .WithIdempotency();
}
