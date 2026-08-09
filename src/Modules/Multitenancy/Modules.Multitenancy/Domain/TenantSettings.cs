using FSH.Framework.Core.Domain;

namespace FSH.Modules.Multitenancy.Domain;

/// <summary>
/// Per-tenant configuration that does not belong on the protected <c>AppTenantInfo</c>
/// (see <c>src/BuildingBlocks/Shared/Multitenancy/AppTenantInfo.cs</c>). Lives in
/// <see cref="Data.TenantDbContext"/> — the tenant catalog, not an isolated per-tenant schema —
/// by the same pattern as <see cref="TenantTheme"/>: an explicit <see cref="TenantId"/> column
/// with a unique index, since <c>TenantDbContext</c> applies no automatic tenant query filter.
/// </summary>
public class TenantSettings : BaseEntity<Guid>, IHasTenant, IAuditableEntity
{
    public const string DefaultTimeZoneId = "UTC";
    public const string DefaultCurrency = "USD";

    public string TenantId { get; private set; } = default!;

    /// <summary>IANA time zone identifier (e.g. "UTC", "Europe/Moscow").</summary>
    public string TimeZoneId { get; set; } = DefaultTimeZoneId;

    /// <summary>ISO 4217 currency code, stored upper-case (e.g. "USD").</summary>
    public string Currency { get; set; } = DefaultCurrency;

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; } = DateTimeOffset.UtcNow;
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private TenantSettings() { } // EF Core

    /// <summary>
    /// Creates settings for a tenant. Falls back to <see cref="DefaultTimeZoneId"/> /
    /// <see cref="DefaultCurrency"/> when the caller doesn't supply a value — the default path
    /// used for the root tenant and for any tenant created without explicit values.
    /// </summary>
    public static TenantSettings Create(
        string tenantId,
        string? timeZoneId = null,
        string? currency = null,
        string? createdBy = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId,
            Currency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency.ToUpperInvariant(),
            CreatedBy = createdBy,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string timeZoneId, string currency, string? modifiedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZoneId);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        TimeZoneId = timeZoneId;
        Currency = currency.ToUpperInvariant();
        LastModifiedOnUtc = DateTimeOffset.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
