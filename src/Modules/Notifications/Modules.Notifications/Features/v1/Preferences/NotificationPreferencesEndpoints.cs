using System.Collections.ObjectModel;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Notifications.Contracts.Authorization;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Notifications.Features.v1.Preferences;

public static class NotificationPreferencesEndpoints
{
    internal static RouteHandlerBuilder MapListNotificationPreferencesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/preferences",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new ListNotificationPreferencesQuery(), ct)))
            .WithName("ListNotificationPreferences")
            .WithSummary("The caller's effective notification preferences (defaults merged with overrides)")
            .RequirePermission(NotificationPermissions.Inbox.View);

    internal static RouteHandlerBuilder MapUpdateNotificationPreferencesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/preferences",
                async (Collection<NotificationPreferenceItem> items, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UpdateNotificationPreferencesCommand(items), ct);
                    return Results.NoContent();
                })
            .WithName("UpdateNotificationPreferences")
            .WithSummary("Set the caller's notification preferences for one or more types")
            .RequirePermission(NotificationPermissions.Inbox.View);
}
