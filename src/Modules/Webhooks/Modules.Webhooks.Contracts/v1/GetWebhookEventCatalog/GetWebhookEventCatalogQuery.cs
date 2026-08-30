using FSH.Modules.Webhooks.Contracts.Catalog;
using Mediator;

namespace FSH.Modules.Webhooks.Contracts.v1.GetWebhookEventCatalog;

/// <summary>
/// Returns the catalog of integration event types a school can subscribe a webhook to, so the
/// admin UI can present a checklist instead of a free-text field. Static reference data — the same
/// list for every tenant.
/// </summary>
public sealed record GetWebhookEventCatalogQuery() : IQuery<IReadOnlyList<WebhookEventType>>;
