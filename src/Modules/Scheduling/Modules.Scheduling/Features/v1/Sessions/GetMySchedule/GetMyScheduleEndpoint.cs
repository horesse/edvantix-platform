using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetMySchedule;

public static class GetMyScheduleEndpoint
{
    internal static RouteHandlerBuilder MapGetMyScheduleEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/sessions/my",
                async (DateTimeOffset from, DateTimeOffset to, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetMyScheduleQuery(from, to), ct)))
            .WithName("GetMySchedule")
            .WithSummary("Get my own schedule")
            .Produces<IReadOnlyList<SessionDto>>()
            .RequirePermission(SchedulingPermissions.Sessions.ViewOwn);
}
