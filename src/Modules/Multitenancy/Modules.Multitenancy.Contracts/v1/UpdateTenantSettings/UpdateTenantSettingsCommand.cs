using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;

public sealed record UpdateTenantSettingsCommand(string TimeZoneId, string Currency) : ICommand;
