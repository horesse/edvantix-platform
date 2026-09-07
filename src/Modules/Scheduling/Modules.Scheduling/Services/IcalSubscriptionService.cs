using System.Buffers.Text;
using System.Security.Cryptography;
using FSH.Framework.Core.Context;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

internal sealed class IcalSubscriptionService(
    SchedulingDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IIcalSubscriptionService
{
    private static readonly TimeSpan LastUsedThrottle = TimeSpan.FromHours(1);

    public Task<IcalSubscriptionToken?> GetForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetUserId();
        return dbContext.IcalSubscriptionTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken);
    }

    public async Task<IcalSubscriptionToken> RotateForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetUserId();

        var existing = await dbContext.IcalSubscriptionTokens
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            existing = IcalSubscriptionToken.Create(userId, NewToken());
            dbContext.IcalSubscriptionTokens.Add(existing);
        }
        else
        {
            existing.Rotate(NewToken());
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing;
    }

    public async Task RevokeForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.GetUserId();

        var existing = await dbContext.IcalSubscriptionTokens
            .FirstOrDefaultAsync(t => t.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return;
        }

        dbContext.IcalSubscriptionTokens.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> ResolveUserIdAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var row = await dbContext.IcalSubscriptionTokens
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        if (row.LastUsedAtUtc is null || now - row.LastUsedAtUtc.Value >= LastUsedThrottle)
        {
            row.MarkUsed(now);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return row.UserId;
    }

    private static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
}
