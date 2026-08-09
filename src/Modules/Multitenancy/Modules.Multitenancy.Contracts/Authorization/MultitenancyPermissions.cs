using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Multitenancy.Contracts.Authorization;

public static class MultitenancyPermissions
{
    public static class Tenants
    {
        public const string Resource = nameof(Tenants);
        public const string View                = $"Permissions.{Resource}.View";
        public const string Create              = $"Permissions.{Resource}.Create";
        public const string Update              = $"Permissions.{Resource}.Update";
        public const string UpgradeSubscription = $"Permissions.{Resource}.UpgradeSubscription";
        public const string ViewTheme           = $"Permissions.{Resource}.ViewTheme";
        public const string UpdateTheme         = $"Permissions.{Resource}.UpdateTheme";
    }

    /// <summary>
    /// Per-school configuration (time zone, currency, ...) — see <c>TenantSettings</c>.
    /// Deliberately its own resource, not <see cref="Tenants"/>: reading it is basic (every
    /// authenticated user needs the school's time zone/currency), while <see cref="Tenants"/>
    /// is operator-only (root). Managing it is tenant-level, not root-level.
    /// </summary>
    public static class SchoolSettings
    {
        public const string Resource = nameof(SchoolSettings);
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Tenants",               ActionConstants.View,                Tenants.Resource, IsRoot: true),
        new("Create Tenants",             ActionConstants.Create,              Tenants.Resource, IsRoot: true),
        new("Update Tenants",             ActionConstants.Update,              Tenants.Resource, IsRoot: true),
        new("Upgrade Tenant Subscription",ActionConstants.UpgradeSubscription, Tenants.Resource, IsRoot: true),
        new("View Tenant Theme",          "ViewTheme",                         Tenants.Resource, IsBasic: true),
        new("Update Tenant Theme",        "UpdateTheme",                       Tenants.Resource),
        new("View School Settings",       ActionConstants.View,                SchoolSettings.Resource, IsBasic: true),
        new("Manage School Settings",     "Manage",                            SchoolSettings.Resource),
    ];
}
