using FSH.Framework.Core.Domain;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A personal, revocable secret that lets an unauthenticated calendar client subscribe to one
/// user's schedule feed — <c>GET /api/v1/my/schedule.ics?tenant=…&amp;token=…</c>. One row per
/// Identity user per tenant (unique index on <see cref="UserId"/>): <see cref="Rotate"/> issues a
/// fresh <see cref="Token"/> into the same row so previously copied links stop working at once, and
/// revoking simply deletes the row. This is deliberately <b>not</b> the general API-key mechanism
/// (see EDX-008) — it authorises exactly one read-only feed and carries no permissions.
/// </summary>
public sealed class IcalSubscriptionToken : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }

    /// <summary>URL-safe opaque secret (Base64Url of 32 random bytes).</summary>
    public string Token { get; private set; } = default!;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>Last time the feed was fetched with this token — updated at most hourly to avoid a
    /// write on every calendar-client poll.</summary>
    public DateTimeOffset? LastUsedAtUtc { get; private set; }

    private IcalSubscriptionToken() { }

    public static IcalSubscriptionToken Create(Guid userId, string token)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return new IcalSubscriptionToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Token = token,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Replace the secret — old links break immediately.</summary>
    public void Rotate(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        LastUsedAtUtc = null;
    }

    public void MarkUsed(DateTimeOffset whenUtc) => LastUsedAtUtc = whenUtc;
}
