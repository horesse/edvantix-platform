using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.DeleteRoom;

public static class DeleteRoomEndpoint
{
    internal static RouteHandlerBuilder MapDeleteRoomEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/rooms/{roomId:guid}",
                async (Guid roomId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteRoomCommand(roomId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteRoom")
            .WithSummary("Delete a room")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Rooms.Manage);
}
