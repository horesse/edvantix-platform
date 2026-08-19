using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.GetRooms;

public static class GetRoomsEndpoint
{
    internal static RouteHandlerBuilder MapGetRoomsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/rooms",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetRoomsQuery(), ct)))
            .WithName("GetRooms")
            .WithSummary("List rooms")
            .Produces<IReadOnlyList<RoomDto>>()
            .RequirePermission(SchedulingPermissions.Rooms.View);
}
