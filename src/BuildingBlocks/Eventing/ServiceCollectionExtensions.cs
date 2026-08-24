using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Inbox;
using FSH.Framework.Eventing.InMemory;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Eventing.RabbitMq;
using FSH.Framework.Eventing.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace FSH.Framework.Eventing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core eventing services (serializer, bus, options).
    /// </summary>
    public static IServiceCollection AddEventingCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<EventingOptions>().BindConfiguration(nameof(EventingOptions));

        services.AddSingleton<IEventSerializer, JsonEventSerializer>();

        // Tenant context for event dispatch (no-op default; multitenancy swaps in a Finbuckle scope)
        // so background publishers establish the tenant before tenant-filtered handler DbContexts build.
        services.TryAddSingleton<IEventTenantScope, NullEventTenantScope>();

        // Register event bus based on configured provider
        var options = configuration.GetSection(nameof(EventingOptions)).Get<EventingOptions>() ?? new EventingOptions();

        if (string.Equals(options.Provider, "RabbitMQ", StringComparison.OrdinalIgnoreCase))
        {
            services.AddOptions<RabbitMqOptions>().BindConfiguration("EventingOptions:RabbitMQ");
            services.AddSingleton<IEventBus, RabbitMqEventBus>();
        }
        else
        {
            // Default to InMemory
            services.AddSingleton<IEventBus, InMemoryEventBus>();
        }

        // Register outbox dispatcher hosted service if enabled
        if (options.UseHostedServiceDispatcher)
        {
            services.AddHostedService<OutboxDispatcherHostedService>();
        }

        return services;
    }

    /// <summary>
    /// Registers EF Core-based outbox and inbox stores for the specified DbContext, keyed by
    /// <typeparamref name="TDbContext"/> so that N modules calling this can coexist — see
    /// <c>.agents/rules/eventing.md</c>. Callers must resolve <c>IOutboxStore</c>/<c>IInboxStore</c>
    /// with <c>[FromKeyedServices(typeof(TDbContext))]</c>; a plain unkeyed injection will fail to
    /// resolve (by design — an unkeyed injection silently picking "whichever module registered
    /// last" is exactly the bug this fixes).
    /// </summary>
    public static IServiceCollection AddEventingForDbContext<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var key = typeof(TDbContext);

        services.AddKeyedScoped<IOutboxStore, EfCoreOutboxStore<TDbContext>>(key);
        services.AddKeyedScoped<IInboxStore, EfCoreInboxStore<TDbContext>>(key);

        // OutboxDispatcher itself must be resolved per-TDbContext too (it wraps that context's own
        // keyed IOutboxStore) — a keyed factory registration, not AddKeyedScoped<TService,TImpl>,
        // because the constructor argument to inject (IOutboxStore) is itself keyed.
        services.AddKeyedScoped<OutboxDispatcher>(key, (sp, _) =>
            ActivatorUtilities.CreateInstance<OutboxDispatcher>(sp, sp.GetRequiredKeyedService<IOutboxStore>(key)));

        // Lets OutboxDispatcherHostedService (and InMemoryEventBus, for the shared Inbox) discover
        // every module that participates in eventing — see EventingDbContextRegistration.
        services.AddSingleton(new EventingDbContextRegistration(key));

        return services;
    }

    /// <summary>
    /// Registers integration event handlers from the specified assemblies.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandlers(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (assemblies is null || assemblies.Length == 0)
        {
            return services;
        }

        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => new
                {
                    Type = t,
                    Interfaces = t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                        .ToArray()
                })
                .Where(x => x.Interfaces.Length > 0);

            foreach (var handler in handlerTypes)
            {
                foreach (var handlerInterface in handler.Interfaces)
                {
                    services.AddScoped(handlerInterface, handler.Type);
                }
            }
        }

        return services;
    }
}