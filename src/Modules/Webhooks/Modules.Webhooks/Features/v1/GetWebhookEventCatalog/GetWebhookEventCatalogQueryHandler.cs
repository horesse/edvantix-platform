using FSH.Modules.Webhooks.Contracts.Catalog;
using FSH.Modules.Webhooks.Contracts.v1.GetWebhookEventCatalog;
using Mediator;

namespace FSH.Modules.Webhooks.Features.v1.GetWebhookEventCatalog;

public sealed class GetWebhookEventCatalogQueryHandler
    : IQueryHandler<GetWebhookEventCatalogQuery, IReadOnlyList<WebhookEventType>>
{
    public ValueTask<IReadOnlyList<WebhookEventType>> Handle(
        GetWebhookEventCatalogQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return ValueTask.FromResult(WebhookEventCatalog.All);
    }
}
