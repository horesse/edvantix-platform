using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace FSH.Modules.Multitenancy.Contracts;

public interface ITenantSettingsService
{
    /// <summary>
    /// Gets the settings for the current tenant context. Falls back to defaults
    /// (UTC / USD) if none exist.
    /// </summary>
    Task<TenantSettingsDto> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the settings for the specified tenant. Falls back to defaults
    /// (UTC / USD) if none exist.
    /// </summary>
    Task<TenantSettingsDto> GetAsync(string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Creates settings for a newly-provisioned tenant. No-op (returns the existing row) if
    /// settings already exist for the tenant — safe to call unconditionally at tenant creation.
    /// A null/empty <paramref name="timeZoneId"/> or <paramref name="currency"/> falls back to
    /// UTC / USD respectively.
    /// </summary>
    Task<TenantSettingsDto> CreateAsync(string tenantId, string? timeZoneId, string? currency, CancellationToken ct = default);

    /// <summary>
    /// Updates the settings for the specified tenant, creating them if they don't exist yet.
    /// </summary>
    Task UpdateAsync(string tenantId, TenantSettingsDto settings, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cached settings for the specified tenant.
    /// </summary>
    Task InvalidateCacheAsync(string tenantId, CancellationToken ct = default);
}
