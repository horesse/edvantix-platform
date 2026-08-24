using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.StudyGroups.Contracts;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Payments.Jobs;

/// <summary>
/// Runs on the 1st of the month — drafts invoices for every <c>Active</c> study group's period,
/// reusing <see cref="BulkGenerateInvoicesCommand"/> through <see cref="IMediator"/> per group (same
/// shape as Scheduling's <c>GenerateSessionsJob</c> iterating active <c>ScheduleTemplate</c>s), never
/// issuing automatically. "Если включено" from docs/02 Модули/Payments.md → «Задания Hangfire» is
/// interpreted as "when the enrollment/course resolves to a tariff" rather than a dedicated
/// per-group settings flag (none exists yet, see docs/04 Задачи/Открытые вопросы.md) —
/// <c>BulkGenerateInvoicesCommand</c> already skips students with no resolvable tariff, so running
/// this unconditionally across every active group is safe: groups with nothing billable simply
/// produce zero drafts.
/// </summary>
public sealed class MonthlyInvoiceDraftJob(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MonthlyInvoiceDraftJob> logger)
{
    /// <summary>Days after the 1st a draft is considered due — a fixed default until
    /// docs/04 Задачи/Открытые вопросы.md settles on a configurable billing-cycle policy.</summary>
    private const int DueDayOfMonth = 10;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var tenants = await tenantStore.GetAllAsync().ConfigureAwait(false);

        int totalCreated = 0;
        foreach (var tenant in tenants)
        {
            if (!tenant.IsActive || string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                totalCreated += await GenerateForTenantAsync(tenant, cancellationToken).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // One tenant's failure must not block the rest of the run
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "[Payments] MonthlyInvoiceDraftJob failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Payments] MonthlyInvoiceDraftJob created/matched {Count} invoice(s) across all tenants", totalCreated);
        }
    }

    private async Task<int> GenerateForTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var setter = (IMultiTenantContextSetter)scope.ServiceProvider
            .GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var studyGroupQueryService = scope.ServiceProvider.GetRequiredService<IStudyGroupQueryService>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var periodFrom = new DateOnly(today.Year, today.Month, 1);
        var periodTo = periodFrom.AddMonths(1).AddDays(-1);
        var dueDate = new DateOnly(today.Year, today.Month, Math.Min(DueDayOfMonth, DateTime.DaysInMonth(today.Year, today.Month)));

        var groupIds = await studyGroupQueryService.GetActiveStudyGroupIdsAsync(cancellationToken).ConfigureAwait(false);

        int created = 0;
        foreach (var groupId in groupIds)
        {
            try
            {
                var invoiceIds = await mediator
                    .Send(new BulkGenerateInvoicesCommand(groupId, periodFrom, periodTo, dueDate, IssueImmediately: false), cancellationToken)
                    .ConfigureAwait(false);
                created += invoiceIds.Count;
            }
#pragma warning disable CA1031 // One group's failure must not block the rest
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(ex, "[Payments] MonthlyInvoiceDraftJob failed for group {StudyGroupId} (tenant {TenantId})", groupId, tenant.Id);
            }
        }

        return created;
    }
}
