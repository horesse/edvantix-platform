using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Multitenancy.Contracts.Authorization;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Notifications.Features.v1.QuietHours;

public static class NotificationQuietHoursEndpoints
{
    internal static RouteHandlerBuilder MapGetNotificationQuietHoursEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/quiet-hours",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetNotificationQuietHoursQuery(), ct)))
            .WithName("GetNotificationQuietHours")
            .WithSummary("The school's quiet-hours window (e-mail is held during it)")
            .RequirePermission(MultitenancyPermissions.SchoolSettings.View);

    internal static RouteHandlerBuilder MapSetNotificationQuietHoursEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/quiet-hours",
                async (SetNotificationQuietHoursCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetNotificationQuietHours")
            .WithSummary("Set the school's quiet-hours window")
            .RequirePermission(MultitenancyPermissions.SchoolSettings.Manage);
}
