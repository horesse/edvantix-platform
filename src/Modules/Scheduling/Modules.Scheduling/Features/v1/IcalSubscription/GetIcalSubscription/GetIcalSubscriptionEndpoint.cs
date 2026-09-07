using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.GetIcalSubscription;

public static class GetIcalSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapGetIcalSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/my/schedule/ical-subscription",
                async (IMediator mediator, CancellationToken ct) =>
                {
                    var dto = await mediator.Send(new GetIcalSubscriptionQuery(), ct);
                    return dto is null ? Results.NoContent() : Results.Ok(dto);
                })
            .WithName("GetIcalSubscription")
            .WithSummary("Get my iCal schedule subscription")
            .WithDescription("Returns the caller's personal calendar-feed token, or 204 if they have none yet.")
            .Produces<IcalSubscriptionDto>()
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(SchedulingPermissions.Sessions.ViewOwn);
}
