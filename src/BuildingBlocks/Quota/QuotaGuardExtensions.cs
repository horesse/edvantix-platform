using FSH.Framework.Shared.Quota;

namespace FSH.Framework.Quota;

/// <summary>
/// Command-handler helper for the soft "plan limit reached" block. Sits in front of a create so a
/// tenant that has hit its plan ceiling for a gauge resource gets a clear 402 instead of silently
/// growing past the limit. Read paths are never gated — existing data stays fully accessible.
/// </summary>
public static class QuotaGuardExtensions
{
    /// <summary>
    /// Throws <see cref="QuotaExceededException"/> (HTTP 402) when adding <paramref name="amount"/>
    /// units of <paramref name="resource"/> would exceed the tenant's plan limit. No-ops when the
    /// resource is unlimited, quotas are disabled, or the tenant is exempt. Does not mutate any
    /// counter — gauge usage is read live from the owning module's store.
    /// </summary>
    public static async ValueTask EnsureHeadroomAsync(
        this IQuotaService quotas,
        string tenantId,
        QuotaResource resource,
        long amount = 1,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(quotas);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var result = await quotas.CheckAsync(tenantId, resource, amount, ct).ConfigureAwait(false);
        if (!result.Allowed)
        {
            throw new QuotaExceededException(resource, result.CurrentUsage, result.Limit);
        }
    }
}
