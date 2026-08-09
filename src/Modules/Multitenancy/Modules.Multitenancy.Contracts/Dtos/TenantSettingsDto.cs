using System.ComponentModel;

namespace FSH.Modules.Multitenancy.Contracts.Dtos;

/// <remarks>
/// Marked <see cref="ImmutableObjectAttribute"/> + <c>sealed</c> so HybridCache can reuse the
/// in-process instance across requests without re-deserializing the JSON payload on every L1 hit.
/// DO NOT add mutable properties or make this class open — it would break HybridCache's L1
/// reuse optimization.
/// </remarks>
[ImmutableObject(true)]
public sealed record TenantSettingsDto
{
    /// <summary>IANA time zone identifier (e.g. "UTC", "Europe/Moscow").</summary>
    public string TimeZoneId { get; init; } = "UTC";

    /// <summary>ISO 4217 currency code (e.g. "USD").</summary>
    public string Currency { get; init; } = "USD";

    public static TenantSettingsDto Default => new();
}
