using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;

public sealed record UpdateTenantSettingsCommand(
    string TimeZoneId,
    string Currency,
    bool RestrictMaterialsOnDebt = false,
    int DebtGraceDays = 7) : ICommand;
