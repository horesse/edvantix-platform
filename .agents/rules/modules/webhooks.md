# Module: Webhooks

Tenant-scoped outbound webhook subscriptions with HMAC-signed delivery and retries. Module `Order = 400`.

**Entities / DbContext:** `WebhookSubscription` (`Url`, `EventsCsv`, `SecretHash`, `IsActive`), `WebhookDelivery` (per-attempt log). `WebhookDbContext` (tenant-filtered). Contracts expose **DTOs only** — `IWebhookDispatcher`/`IWebhookDeliveryService` are internal.
**Areas:** Create/Delete/Get subscriptions, GetDeliveries, Test, GetEventCatalog. Full list: `Features/v1/` or `/scalar`.

**Event catalog:** `WebhookEventCatalog.All` (Contracts, `Catalog/`) is the discovery list for `GET /event-types` — 24 events, `Name` = the integration-event contract's simple type name (`typeof(TEvent).Name`), i.e. exactly the fan-out selector. It is **not an allow-list**: `CreateWebhookSubscriptionCommandValidator` only rejects blank event tokens, so a subscription can still name an uncatalogued (future) event; `*` matches all.

## Gotchas

- **Fan-out is an open-generic handler** — `WebhookFanoutHandler<TEvent>` is registered as an open generic, so it handles **every** `IIntegrationEvent` with no per-event wiring. It skips events with null `TenantId` (subscriptions are tenant-only) and matches event-type name against each subscription's `EventsCsv` (`*` wildcard supported).
- **Restore tenant context in the background** — both the fan-out handler and `WebhookDispatchJob` set `IMultiTenantContext` in a fresh scope before reading the tenant-filtered DbContext (background pumps/Hangfire carry no HTTP context). This is the canonical pattern for any background reader of tenant data — see `eventing.md`, `jobs.md`.
- **HMAC signing** — `X-Webhook-Signature: sha256=<hex HMACSHA256>` (`WebhookPayloadSigner.Sign`), plus `X-Webhook-Event` and `X-Webhook-Delivery-Id` headers.
- **Body is the envelope, not the raw event** — the fan-out wraps the serialized event in `WebhookEnvelope` (Contracts, `WebhookPayloads/v1/`): `{ id, type, occurredAt, tenantId, data }`, where `data` is the event JSON minus the transport keys (`id`, `occurredOnUtc`, `tenantId`, `correlationId`, `source`). Built by the static `WebhookEnvelopeBuilder`. `DeliverAsync`/`DispatchAsync` stay payload-agnostic (they take a ready string), so only the fan-out and the `/test` handler build envelopes.
- **Delivery** — `WebhookDispatcher.EnqueueAsync` enqueues a Hangfire `WebhookDispatchJob` per subscription; `[AutomaticRetry(Attempts=4, DelaysInSeconds={30,120,600,3600})]`. Transient (5xx/408/429) throws to reschedule; permanent 4xx completes silently. Each attempt persists a `WebhookDelivery` row. The `"Webhooks"` HttpClient uses `AddHeroResilience` (see `resilience.md`).
