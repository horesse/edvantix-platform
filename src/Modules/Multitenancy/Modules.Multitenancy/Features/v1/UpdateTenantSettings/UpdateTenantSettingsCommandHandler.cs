using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.UpdateTenantSettings;

public sealed class UpdateTenantSettingsCommandHandler(
    ITenantSettingsService settingsService,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<UpdateTenantSettingsCommand>
{
    public async ValueTask<Unit> Handle(UpdateTenantSettingsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id
            ?? throw new InvalidOperationException("No tenant context available");

        // A null InvoiceNumberTemplate means "keep what's there" — resolve it against the current
        // (cache-backed) settings so we never write a blank/placeholder over a customised template.
        var current = await settingsService.GetAsync(tenantId, cancellationToken);

        var settings = new TenantSettingsDto
        {
            TimeZoneId = command.TimeZoneId,
            Currency = command.Currency,
            RestrictMaterialsOnDebt = command.RestrictMaterialsOnDebt,
            DebtGraceDays = command.DebtGraceDays,
            InvoiceNumberTemplate = string.IsNullOrWhiteSpace(command.InvoiceNumberTemplate)
                ? current.InvoiceNumberTemplate
                : command.InvoiceNumberTemplate.Trim(),
        };

        await settingsService.UpdateAsync(tenantId, settings, cancellationToken);

        return Unit.Value;
    }
}
