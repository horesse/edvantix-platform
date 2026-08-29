namespace FSH.Modules.Notifications.Channels;

/// <summary>
/// The delivery channels a notification can go out on. A <c>[Flags]</c> combination on a request;
/// a single bit on an <see cref="INotificationChannel"/>. Schools will ask for Telegram and SMS —
/// add a bit and a channel implementation, nothing else changes.
/// </summary>
[Flags]
public enum NotificationChannelKind
{
    None = 0,

    /// <summary>The in-app bell inbox (a <c>Notification</c> row + live SignalR push).</summary>
    InApp = 1,

    /// <summary>E-mail via <c>BuildingBlocks/Mailing</c>. Only fires when the template has an e-mail body.</summary>
    Email = 2,

    All = InApp | Email,
}
