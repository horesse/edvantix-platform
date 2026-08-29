using FSH.Framework.Core.Domain;

namespace FSH.Modules.Notifications.Domain;

/// <summary>
/// One user's opt-in/opt-out for a single notification type, per channel. Absent row → the
/// <c>NotificationDefaults</c> for that type/channel. One row per (UserId, Type).
/// </summary>
public sealed class NotificationPreference : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;

    /// <summary>A <c>NotificationTypes</c> key.</summary>
    public string Type { get; private set; } = default!;

    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference Create(string userId, string type, bool inAppEnabled, bool emailEnabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return new NotificationPreference
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Type = type,
            InAppEnabled = inAppEnabled,
            EmailEnabled = emailEnabled,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Set(bool inAppEnabled, bool emailEnabled)
    {
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
