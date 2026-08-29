using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using FSH.Modules.Notifications.Templating;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Notifications.Features.v1.Preferences;

/// <summary>Reads/writes per-user notification opt-ins and resolves the effective channel mask for a dispatch.</summary>
public interface INotificationPreferenceService
{
    /// <summary><paramref name="requested"/> masked down to the channels this user has enabled for <paramref name="type"/>.</summary>
    Task<NotificationChannelKind> EffectiveChannelsAsync(
        string userId, string type, NotificationChannelKind requested, CancellationToken ct = default);

    Task<IReadOnlyList<NotificationPreferenceDto>> GetEffectiveAsync(string userId, CancellationToken ct = default);

    Task UpsertAsync(string userId, IReadOnlyCollection<NotificationPreferenceItem> items, CancellationToken ct = default);
}

public sealed class NotificationPreferenceService(NotificationsDbContext db) : INotificationPreferenceService
{
    public async Task<NotificationChannelKind> EffectiveChannelsAsync(
        string userId, string type, NotificationChannelKind requested, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || requested == NotificationChannelKind.None)
        {
            return requested;
        }

        var row = await db.NotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == type, ct)
            .ConfigureAwait(false);

        var inAppOn = row?.InAppEnabled ?? NotificationDefaults.IsOn(type, NotificationChannelKind.InApp);
        var emailOn = row?.EmailEnabled ?? NotificationDefaults.IsOn(type, NotificationChannelKind.Email);

        var allowed = NotificationChannelKind.None;
        if (inAppOn) allowed |= NotificationChannelKind.InApp;
        if (emailOn) allowed |= NotificationChannelKind.Email;

        return requested & allowed;
    }

    public async Task<IReadOnlyList<NotificationPreferenceDto>> GetEffectiveAsync(string userId, CancellationToken ct = default)
    {
        var rows = await db.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => p.Type, ct)
            .ConfigureAwait(false);

        return NotificationTemplateCatalog.Keys
            .OrderBy(t => t, StringComparer.Ordinal)
            .Select(type => rows.TryGetValue(type, out var row)
                ? new NotificationPreferenceDto(type, row.InAppEnabled, row.EmailEnabled)
                : new NotificationPreferenceDto(
                    type,
                    NotificationDefaults.IsOn(type, NotificationChannelKind.InApp),
                    NotificationDefaults.IsOn(type, NotificationChannelKind.Email)))
            .ToList();
    }

    public async Task UpsertAsync(
        string userId, IReadOnlyCollection<NotificationPreferenceItem> items, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0)
        {
            return;
        }

        var types = items.Select(i => i.Type).ToHashSet(StringComparer.Ordinal);
        var existing = await db.NotificationPreferences
            .Where(p => p.UserId == userId && types.Contains(p.Type))
            .ToDictionaryAsync(p => p.Type, ct)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            if (existing.TryGetValue(item.Type, out var row))
            {
                row.Set(item.InApp, item.Email);
            }
            else
            {
                db.NotificationPreferences.Add(
                    NotificationPreference.Create(userId, item.Type, item.InApp, item.Email));
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
