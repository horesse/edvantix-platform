using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantSettings;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenantSettings;

public sealed class GetTenantSettingsQueryHandler(ITenantSettingsService settingsService)
    : IQueryHandler<GetTenantSettingsQuery, TenantSettingsDto>
{
    public async ValueTask<TenantSettingsDto> Handle(GetTenantSettingsQuery query, CancellationToken cancellationToken)
    {
        return await settingsService.GetCurrentAsync(cancellationToken);
    }
}
