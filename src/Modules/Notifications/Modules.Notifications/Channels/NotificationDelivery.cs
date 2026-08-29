using FSH.Modules.Notifications.Templating;

namespace FSH.Modules.Notifications.Channels;

/// <summary>
/// One rendered notification ready to be handed to every enabled <see cref="INotificationChannel"/>.
/// The template is rendered once by <see cref="INotificationDispatcher"/>; channels only deliver.
/// </summary>
public sealed record NotificationDelivery(
    string RecipientUserId,
    string? RecipientEmail,
    string Type,
    string Source,
    RenderedNotification Content,
    object? Metadata);
