namespace FSH.Modules.Webhooks.Contracts.Catalog;

/// <summary>
/// One entry in the catalog of event types a tenant can subscribe a webhook to.
/// <see cref="Name"/> is the stable selector stored in <c>WebhookSubscription.EventsCsv</c> and
/// echoed back on delivery in the <c>X-Webhook-Event</c> header — it is the simple type name of the
/// publishing integration event contract (e.g. <c>StudentEnrolledIntegrationEvent</c>), which is
/// exactly what the open-generic fan-out handler matches on.
/// </summary>
public sealed record WebhookEventType(string Name, string Module, string Description);
