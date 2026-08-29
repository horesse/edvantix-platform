namespace FSH.Modules.Notifications.Channels;

/// <summary>
/// A delivery mechanism for a notification (in-app inbox, e-mail, later Telegram/SMS). Registered
/// once per implementation; <see cref="INotificationDispatcher"/> resolves them all and calls the
/// ones the request asked for.
///
/// Contract: the in-app channel may throw (a failed inbox write should surface to the originating
/// request — the in-memory bus runs handlers synchronously). Outbound channels (e-mail, …) must be
/// best-effort: a transport failure is logged, never thrown, so it cannot fail the create/scan that
/// raised the event.
/// </summary>
public interface INotificationChannel
{
    /// <summary>The single channel bit this implementation serves.</summary>
    NotificationChannelKind Kind { get; }

    Task SendAsync(NotificationDelivery delivery, CancellationToken ct = default);
}
