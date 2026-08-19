namespace FSH.Framework.Eventing.Outbox;

/// <summary>
/// Marks a <c>DbContext</c> type as having outbox/inbox stores registered via
/// <see cref="ServiceCollectionExtensions.AddEventingForDbContext{TDbContext}"/>. One instance is
/// added per call, as a singleton — <see cref="OutboxDispatcherHostedService"/> enumerates all of
/// them to drain every module's outbox on each tick (not just one), and
/// <c>FSH.Framework.Eventing.InMemory.InMemoryEventBus</c> uses the first one registered as the
/// shared Inbox ledger. See <c>.agents/rules/eventing.md</c> for the full rationale — this exists
/// because <see cref="Abstractions.IIntegrationEvent"/>'s outbox/inbox stores are keyed by
/// <see cref="DbContextType"/> (one physical table per module), so nothing else lets the framework
/// discover "every module that participates in eventing" without this list.
/// </summary>
/// <param name="DbContextType">The module's <c>DbContext</c> type — also the DI key for its
/// <c>IOutboxStore</c>/<c>IInboxStore</c>/<c>OutboxDispatcher</c> registrations.</param>
public sealed record EventingDbContextRegistration(Type DbContextType);
