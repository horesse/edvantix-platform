using Mediator;

namespace FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;

// EDX-013 — InvoiceNumberTemplate: null leaves the tenant's current template unchanged, so callers
// that only manage time zone / currency need not echo it back.
public sealed record UpdateTenantSettingsCommand(
    string TimeZoneId,
    string Currency,
    bool RestrictMaterialsOnDebt = false,
    int DebtGraceDays = 7,
    string? InvoiceNumberTemplate = null) : ICommand;
