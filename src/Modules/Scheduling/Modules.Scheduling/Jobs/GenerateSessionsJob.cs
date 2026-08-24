using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Scheduling.Jobs;

/// <summary>
/// Daily recurring job that keeps the generation horizon topped up for every active
/// <see cref="Domain.ScheduleTemplate"/> across every tenant (see
/// docs/02 Модули/Scheduling.md → «Генерация», docs/04 Задачи/Открытые вопросы.md → "Горизонт
/// генерации" — default 8 weeks). Reuses <see cref="GenerateSessionsCommand"/> through
/// <see cref="IMediator"/> rather than duplicating the generator's logic, so a manual
/// <c>POST /schedule-templates/{id}/generate</c> and this job behave identically (same conflict
/// handling, same idempotency, same events).
/// <para>
/// A fresh DI scope is created **per tenant**: <see cref="SchedulingDbContext"/> is tenant-filtered,
/// and a background job carries no ambient tenant context, so the Finbuckle context has to be
/// installed before that scope's <see cref="SchedulingDbContext"/> is ever touched (see
/// <c>.agents/rules/jobs.md</c>). One tenant's failure is logged and does not stop the rest — same
/// as Multitenancy's <c>TenantExpiryScanJob</c>.
/// </para>
/// </summary>
public sealed class GenerateSessionsJob(
    IMultiTenantStore<AppTenantInfo> tenantStore,
    IServiceScopeFactory scopeFactory,
    ILogger<GenerateSessionsJob> logger)
{
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
                logger.LogError(ex, "[Scheduling] GenerateSessionsJob failed for tenant {TenantId}", tenant.Id);
            }
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Scheduling] GenerateSessionsJob created {Count} session(s) across all tenants", totalCreated);
        }
    }

    private async Task<int> GenerateForTenantAsync(AppTenantInfo tenant, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var setter = (IMultiTenantContextSetter)scope.ServiceProvider
            .GetRequiredService<IMultiTenantContextAccessor<AppTenantInfo>>();
        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var templateIds = await dbContext.ScheduleTemplates
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int created = 0;
        foreach (var templateId in templateIds)
        {
            try
            {
                var result = await mediator.Send(new GenerateSessionsCommand(templateId, HorizonWeeks: null), cancellationToken)
                    .ConfigureAwait(false);
                created += result.CreatedSessionIds.Count;
            }
#pragma warning disable CA1031 // One template's failure (e.g. its group isn't Active) must not block the rest
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(
                    ex, "[Scheduling] GenerateSessionsJob failed for template {TemplateId} (tenant {TenantId})",
                    templateId, tenant.Id);
            }
        }

        return created;
    }
}
