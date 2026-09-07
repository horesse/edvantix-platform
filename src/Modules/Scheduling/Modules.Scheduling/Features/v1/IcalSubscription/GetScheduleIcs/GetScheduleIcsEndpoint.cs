using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.GetScheduleIcs;

public static class GetScheduleIcsEndpoint
{
    internal static RouteHandlerBuilder MapGetScheduleIcsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/my/schedule.ics",
                async (
                    [FromQuery] string? token,
                    [FromQuery] DateTimeOffset? from,
                    [FromQuery] DateTimeOffset? to,
                    IMediator mediator,
                    CancellationToken ct) =>
                {
                    var body = await mediator.Send(new GetScheduleIcsQuery(token ?? string.Empty, from, to), ct);
                    return body is null
                        ? Results.NotFound()
                        : Results.Text(body, "text/calendar; charset=utf-8");
                })
            .WithName("GetScheduleIcs")
            .WithSummary("iCal feed of my upcoming schedule")
            .WithDescription(
                "Anonymous calendar feed. Authenticated by a personal `token` (see " +
                "`POST /my/schedule/ical-subscription/rotate`); the tenant is taken from `?tenant=`.")
            .Produces(StatusCodes.Status200OK, contentType: "text/calendar")
            .Produces(StatusCodes.Status404NotFound)
            .AllowAnonymous()
            .RequireRateLimiting("auth");
}
