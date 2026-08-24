# Eventing — domain events, integration events, Outbox/Inbox

Read before publishing/handling cross-module events. `src/BuildingBlocks/Eventing/`.

## Two tiers

- **Domain events** (in-process, pre-commit) — inherit `DomainEvent` (record: `EventId`, `OccurredOnUtc`, `CorrelationId`, `TenantId`). Raised on aggregates (`IHasDomainEvents`).
- **Integration events** (cross-module, async) — implement `IIntegrationEvent` (`Id`, `OccurredOnUtc`, `TenantId`, `CorrelationId`, `Source`). Handlers implement `IIntegrationEventHandler<T>` (single `HandleAsync(T, ct)`), are `sealed`, live in `Events/` or `IntegrationEventHandlers/`.

## The Outbox is the only way to publish

**Do not call `IEventBus` directly from a handler.** Publish via the outbox so it commits in the same transaction and survives crashes:

```csharp
await _outboxStore.AddAsync(integrationEvent, ct).ConfigureAwait(false);
```

`EfCoreOutboxStore.AddAsync` serializes + `SaveChanges` immediately. `OutboxDispatcherHostedService` polls every `OutboxDispatchIntervalSeconds` (default 10), `OutboxDispatcher` pulls a batch (`OutboxBatchSize`, default 100), publishes via `IEventBus`, and dead-letters after `OutboxMaxRetries` (default 5) → `IsDead`. `OutboxMessage`/`InboxMessage` are `IGlobalEntity` (no tenant filter — the dispatcher has no tenant context; `TenantId` is an explicit column).

## Idempotency is free (in-memory bus)

`InMemoryEventBus` resolves handlers in a fresh DI scope and applies the **Inbox**: skips if `IInboxStore.HasProcessedAsync(eventId, handlerName)`, marks processed after success. Composite key `{Id, HandlerName}`; concurrent-insert race is swallowed. Don't hand-roll dedup.

## Wiring (3 calls in the module's `ConfigureServices`)

```csharp
services.AddEventingCore(builder.Configuration);                        // serializer + bus + hosted dispatcher — call ONCE, in IdentityModule only
services.AddEventingForDbContext<MyDbContext>();                        // outbox/inbox stores (scoped, keyed by typeof(MyDbContext))
services.AddIntegrationEventHandlers(typeof(MyModule).Assembly);        // scans IIntegrationEventHandler<>
```

Bus = `EventingOptions.Provider`: `"RabbitMQ"` → `RabbitMqEventBus` (durable topic exchange); else `InMemoryEventBus` (default).

## `IOutboxStore`/`IInboxStore` are keyed by `TDbContext` — inject with `[FromKeyedServices]`

`AddEventingForDbContext<TDbContext>()` registers `IOutboxStore`/`IInboxStore`/`OutboxDispatcher` as
**keyed** services, key = `typeof(TDbContext)`. This exists because N modules all call this method
with a *different* `TDbContext`, and a plain unkeyed `AddScoped<IOutboxStore, ...>` from each module
would mean .NET DI silently resolves every unkeyed `IOutboxStore` injection anywhere in the app to
whichever module registered **last** — the bug this fixed (see
`docs/04 Задачи/Задачи · Доработки каркаса.md` → "Eventing (BuildingBlocks)" for the incident: an
Identity handler was writing `UserRegisteredIntegrationEvent` through a `StudyGroupsDbContext`
instance it never otherwise touched — a **separate, non-atomic transaction** from the handler's own
`SaveChanges`, not just "wrong schema").

**Every handler that publishes an event must annotate the parameter**, not just declare the type:

```csharp
public sealed class CreateFooCommandHandler(
    MyDbContext dbContext,
    [FromKeyedServices(typeof(MyDbContext))] IOutboxStore outboxStore,   // <-- required
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<CreateFooCommand, Guid>
{ ... }
```

A plain `IOutboxStore outboxStore` parameter (no attribute) throws `InvalidOperationException` at
resolution time — by design, so a missed annotation fails loudly at first request instead of quietly
writing to someone else's table. Requires `using Microsoft.Extensions.DependencyInjection;` for
`FromKeyedServicesAttribute`.

`OutboxDispatcherHostedService` already knows to drain every registered `TDbContext`'s outbox each
tick (it enumerates `EventingDbContextRegistration`, one singleton per `AddEventingForDbContext` call)
— nothing to do there when adding a module.

**Inbox dedup stays intentionally centralized**, not keyed per-consumer: `InMemoryEventBus` resolves
`IInboxStore` for the FIRST module that called `AddEventingForDbContext` (deterministic — Identity,
order 1), because a handler's dedup key (`eventId` + handler type FQN) is globally unique regardless
of which physical table stores it, and the bus has no reliable way to know which module "owns" an
arbitrary `IIntegrationEventHandler<T>` it resolves via reflection. Don't try to key Inbox lookups
per-handler without redesigning that mapping — it isn't needed for correctness.

## Gotchas

- **Renaming/moving an integration event type breaks deserialization** — the outbox stores the assembly-qualified type name; `Type.GetType()` returns null → the message dead-letters. Keep event type names/namespaces stable, or migrate dead rows.
- **Background handlers carry no HTTP/tenant context.** An open-generic or background handler that reads a tenant-filtered DbContext must restore Finbuckle context first via `IMultiTenantContextSetter` (see `WebhookFanoutHandler`, `modules/webhooks.md`).
- In-memory bus runs handlers **synchronously in the publisher's scope** — keep handler work minimal; exceptions surface to the originating request (relevant for Notifications consuming Chat events).
- Set `UseHostedServiceDispatcher=false` to drive the outbox via Hangfire instead of the hosted service.
