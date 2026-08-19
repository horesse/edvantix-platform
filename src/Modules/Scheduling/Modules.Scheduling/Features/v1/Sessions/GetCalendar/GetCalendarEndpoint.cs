using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetCalendar;

public static class GetCalendarEndpoint
{
    internal static RouteHandlerBuilder MapGetCalendarEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/sessions/calendar",
                async (
                    DateTimeOffset from,
                    DateTimeOffset to,
                    Guid? studyGroupId,
                    Guid? teacherId,
                    Guid? roomId,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetCalendarQuery(from, to, studyGroupId, teacherId, roomId), ct)))
            .WithName("GetCalendar")
            .WithSummary("Get an aggregated calendar view")
            .Produces<IReadOnlyList<CalendarEntryDto>>()
            .RequirePermission(SchedulingPermissions.Sessions.View);
}
