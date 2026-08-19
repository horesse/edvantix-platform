using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Scheduling.Jobs;

/// <summary>
/// Hourly recurring job — publishes <see cref="SessionReminderDueIntegrationEvent"/> for every
/// <c>Planned</c> session starting in the [23h, 24h) window from now. That one-hour bucket (not "in
/// the next 24h", re-evaluated every run) is what keeps this idempotent without a "reminder sent"
/// flag: assuming the job doesn't miss a run, each session falls into exactly one hourly bucket.
/// Same per-tenant fresh-scope pattern as <see cref="GenerateSessionsJob"/>.
/// </summary>
public sealed class SessionReminderJob(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<SessionReminderJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tenants = await tenantStore.GetAllAsync().ConfigureAwait(false);

        int totalPublished = 0;
        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive || string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                totalPublished += await RemindForTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // One tenant's failure must not block the rest of the run
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "[Scheduling] SessionReminderJob failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Scheduling] SessionReminderJob published {Count} reminder(s) across all tenants", totalPublished);
        }
    }

    private async Task<int> RemindForTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var setter = (IMultiTenantContextSetter)scope.ServiceProvider
            .GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var outboxStore = scope.ServiceProvider.GetRequiredKeyedService<IOutboxStore>(typeof(SchedulingDbContext));

        var now = timeProvider.GetUtcNow();
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(24);

        var sessions = await dbContext.Sessions
            .Where(s => s.Status == SessionStatus.Planned && s.StartUtc >= windowStart && s.StartUtc < windowEnd)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var session in sessions)
        {
            await outboxStore.AddAsync(
                new SessionReminderDueIntegrationEvent(
                    Id: Guid.NewGuid(),
                    OccurredOnUtc: now.UtcDateTime,
                    TenantId: tenant.Id,
                    CorrelationId: Guid.NewGuid().ToString(),
                    Source: "Scheduling",
                    SessionId: session.Id,
                    StudyGroupId: session.StudyGroupId,
                    StartUtc: session.StartUtc),
                cancellationToken).ConfigureAwait(false);
        }

        return sessions.Count;
    }
}
