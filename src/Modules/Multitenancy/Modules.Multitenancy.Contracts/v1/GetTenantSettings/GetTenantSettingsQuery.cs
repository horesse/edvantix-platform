using FSH.Modules.Multitenancy.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.GetTenantSettings;

public sealed record GetTenantSettingsQuery : IQuery<TenantSettingsDto>;
