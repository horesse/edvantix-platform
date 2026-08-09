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

        var settings = new TenantSettingsDto
        {
            TimeZoneId = command.TimeZoneId,
            Currency = command.Currency,
        };

        await settingsService.UpdateAsync(tenantId, settings, cancellationToken);

        return Unit.Value;
    }
}
