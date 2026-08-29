using FSH.Framework.Core.Domain;

namespace FSH.Modules.Notifications.Domain;

/// <summary>
/// School-wide "do not e-mail" window, in the school's local time (<c>TenantSettings.TimeZoneId</c>).
/// One row per tenant. During the window the in-app bell still updates; only e-mail is held back.
/// A window with <see cref="StartLocal"/> &gt; <see cref="EndLocal"/> spans midnight.
/// </summary>
public sealed class NotificationQuietHours : AggregateRoot<Guid>
{
    public bool Enabled { get; private set; }
    public TimeOnly StartLocal { get; private set; }
    public TimeOnly EndLocal { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private NotificationQuietHours() { }

    public static NotificationQuietHours Create(bool enabled, TimeOnly startLocal, TimeOnly endLocal) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Enabled = enabled,
            StartLocal = startLocal,
            EndLocal = endLocal,
            UpdatedAtUtc = DateTime.UtcNow,
        };

    public void Set(bool enabled, TimeOnly startLocal, TimeOnly endLocal)
    {
        Enabled = enabled;
        StartLocal = startLocal;
        EndLocal = endLocal;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>True when <paramref name="localNow"/> falls inside the window (handles the midnight-spanning case).</summary>
    public bool Contains(TimeOnly localNow)
    {
        if (!Enabled || StartLocal == EndLocal)
        {
            return false;
        }

        return StartLocal < EndLocal
            ? localNow >= StartLocal && localNow < EndLocal
            : localNow >= StartLocal || localNow < EndLocal;
    }
}
