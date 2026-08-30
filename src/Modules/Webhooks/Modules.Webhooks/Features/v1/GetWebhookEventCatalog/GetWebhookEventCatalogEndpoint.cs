using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Webhooks.Contracts.Authorization;
using FSH.Modules.Webhooks.Contracts.Catalog;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookEventCatalog;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookEventCatalog;

public static class GetWebhookEventCatalogEndpoint
{
    internal static RouteHandlerBuilder MapGetWebhookEventCatalogEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/event-types", async (IMediator mediator, CancellationToken ct) =>
            TypedResults.Ok(await mediator.Send(new GetWebhookEventCatalogQuery(), ct)))
        .WithName("GetWebhookEventCatalog")
        .WithSummary("List subscribable webhook event types")
        .WithDescription("The catalog of integration event types a subscription can listen for. The event name is echoed on delivery in the X-Webhook-Event header; \"*\" subscribes to all of them.")
        .RequirePermission(WebhooksPermissions.Subscriptions.View)
        .Produces<IReadOnlyList<WebhookEventType>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
    }
}
