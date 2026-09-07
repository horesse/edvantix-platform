using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.RotateIcalSubscription;

public static class RotateIcalSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapRotateIcalSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/my/schedule/ical-subscription/rotate",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new RotateIcalSubscriptionCommand(), ct)))
            .WithName("RotateIcalSubscription")
            .WithSummary("Create or regenerate my iCal schedule subscription")
            .WithDescription("Issues a fresh feed token; any link copied earlier stops working.")
            .Produces<IcalSubscriptionDto>()
            .RequirePermission(SchedulingPermissions.Sessions.ViewOwn);
}
