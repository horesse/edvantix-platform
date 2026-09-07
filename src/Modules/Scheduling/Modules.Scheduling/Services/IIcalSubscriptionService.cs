using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Services;

/// <summary>Owns the per-user <see cref="IcalSubscriptionToken"/> lifecycle: read/rotate/revoke for
/// the current JWT caller, plus token→user resolution for the anonymous feed. Tenant scoping is the
/// <c>SchedulingDbContext</c> filter (the feed request carries <c>?tenant=</c>, resolved by
/// Finbuckle's delegate strategy before the DbContext is built).</summary>
public interface IIcalSubscriptionService
{
    Task<IcalSubscriptionToken?> GetForCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Create the row if absent, otherwise replace the secret. Returns the live row.</summary>
    Task<IcalSubscriptionToken> RotateForCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Delete the current user's row (no-op if none). Every copied link stops working.</summary>
    Task RevokeForCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Resolve a feed token to its owner's Identity user id, or <c>null</c> if unknown.
    /// Also stamps <c>LastUsedAtUtc</c> (throttled to at most hourly).</summary>
    Task<Guid?> ResolveUserIdAsync(string token, CancellationToken cancellationToken = default);
}
