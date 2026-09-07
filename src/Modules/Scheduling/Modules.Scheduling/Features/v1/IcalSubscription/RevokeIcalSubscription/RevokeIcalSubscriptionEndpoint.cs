using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.RevokeIcalSubscription;

public static class RevokeIcalSubscriptionEndpoint
{
    internal static RouteHandlerBuilder MapRevokeIcalSubscriptionEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/my/schedule/ical-subscription",
                async (IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RevokeIcalSubscriptionCommand(), ct);
                    return Results.NoContent();
                })
            .WithName("RevokeIcalSubscription")
            .WithSummary("Revoke my iCal schedule subscription")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(SchedulingPermissions.Sessions.ViewOwn);
}
