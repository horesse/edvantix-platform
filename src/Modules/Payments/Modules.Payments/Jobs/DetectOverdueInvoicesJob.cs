using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.Events;
using FSH.Modules.Payments.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Payments.Jobs;

/// <summary>
/// Daily recurring job — publishes <see cref="StudentInvoiceOverdueIntegrationEvent"/> for every
/// <c>Issued</c>/<c>PartiallyPaid</c> invoice whose <c>DueDate</c> is in the past. Fires again every
/// day an invoice remains overdue (no "already notified" flag) — each day's amount/day-count is
/// fresh information, unlike Scheduling's <c>SessionReminderJob</c> one-shot hourly bucket.
/// Per-tenant fresh scope, same pattern as <c>GenerateSessionsJob</c>/<c>SessionReminderJob</c>.
/// </summary>
public sealed class DetectOverdueInvoicesJob(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<DetectOverdueInvoicesJob> logger)
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
                totalPublished += await DetectForTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // One tenant's failure must not block the rest of the run
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "[Payments] DetectOverdueInvoicesJob failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Payments] DetectOverdueInvoicesJob published {Count} overdue notice(s) across all tenants", totalPublished);
        }
    }

    private async Task<int> DetectForTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var setter = (IMultiTenantContextSetter)scope.ServiceProvider
            .GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var outboxStore = scope.ServiceProvider.GetRequiredKeyedService<IOutboxStore>(typeof(PaymentsDbContext));

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var overdue = await dbContext.StudentInvoices
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) && i.DueDate < today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        foreach (var invoice in overdue)
        {
            await outboxStore.AddAsync(
                new StudentInvoiceOverdueIntegrationEvent(
                    Guid.NewGuid(), now.UtcDateTime, tenant.Id, Guid.NewGuid().ToString(), "Payments",
                    invoice.Id, invoice.StudentId, invoice.Total - invoice.PaidAmount, today.DayNumber - invoice.DueDate.DayNumber),
                cancellationToken).ConfigureAwait(false);
        }

        if (overdue.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return overdue.Count;
    }
}
