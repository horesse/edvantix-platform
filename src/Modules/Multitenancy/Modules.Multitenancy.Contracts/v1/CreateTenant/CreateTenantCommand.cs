using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.CreateTenant;

// TimeZoneId / Currency: IANA time zone / ISO 4217 currency code for the new school.
// Null/empty falls back to UTC / USD respectively (see CreateTenantCommandHandler).
public sealed record CreateTenantCommand(
    string Id,
    string Name,
    string? ConnectionString,
    string AdminEmail,
    string AdminPassword,
    string? Issuer,
    string? PlanKey = null,
    string? TimeZoneId = null,
    string? Currency = null) : ICommand<CreateTenantCommandResponse>;