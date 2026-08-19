using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.UpdateRoom;

public static class UpdateRoomEndpoint
{
    internal static RouteHandlerBuilder MapUpdateRoomEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/rooms/{roomId:guid}",
                async (Guid roomId, UpdateRoomBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateRoomCommand(roomId, body.Name, body.Capacity, body.Location, body.IsVirtual), ct);
                    return Results.NoContent();
                })
            .WithName("UpdateRoom")
            .WithSummary("Update a room")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Rooms.Manage);

    public sealed record UpdateRoomBody(string Name, int Capacity, string? Location, bool IsVirtual);
}
