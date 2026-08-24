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
/// Daily recurring job — publishes <see cref="StudentInvoiceDueSoonIntegrationEvent"/> for every
/// <c>Issued</c>/<c>PartiallyPaid</c> invoice whose <c>DueDate</c> is exactly
/// <see cref="ReminderDays"/> days away. A fixed single-day trigger point (not "due within N days,
/// re-evaluated daily") is what keeps this idempotent without a "reminder sent" flag — same
/// reasoning as Scheduling's <c>SessionReminderJob</c> hourly bucket.
/// </summary>
public sealed class PaymentReminderJob(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<PaymentReminderJob> logger)
{
    private const int ReminderDays = 3;

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
                logger.LogError(ex, "[Payments] PaymentReminderJob failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Payments] PaymentReminderJob published {Count} reminder(s) across all tenants", totalPublished);
        }
    }

    private async Task<int> RemindForTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var setter = (IMultiTenantContextSetter)scope.ServiceProvider
            .GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        var outboxStore = scope.ServiceProvider.GetRequiredKeyedService<IOutboxStore>(typeof(PaymentsDbContext));

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var dueDate = today.AddDays(ReminderDays);

        var dueSoon = await dbContext.StudentInvoices
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) && i.DueDate == dueDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        foreach (var invoice in dueSoon)
        {
            await outboxStore.AddAsync(
                new StudentInvoiceDueSoonIntegrationEvent(
                    Guid.NewGuid(), now.UtcDateTime, tenant.Id, Guid.NewGuid().ToString(), "Payments",
                    invoice.Id, invoice.StudentId, invoice.PayerGuardianId, invoice.Total - invoice.PaidAmount, ReminderDays),
                cancellationToken).ConfigureAwait(false);
        }

        if (dueSoon.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return dueSoon.Count;
    }
}
