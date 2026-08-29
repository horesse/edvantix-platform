using FSH.Framework.Web.Realtime;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.Channels;

/// <summary>
/// Writes the bell-inbox row and pushes <c>NotificationCreated</c> to the recipient's SignalR group
/// (<c>user:{userId}</c>) so the badge updates live. Deliberately not best-effort: a failed write
/// surfaces to the request that raised the event.
/// </summary>
public sealed class InAppNotificationChannel(
    NotificationsDbContext db,
    IHubContext<AppHub> hub,
    ILogger<InAppNotificationChannel> logger)
    : INotificationChannel
{
    public NotificationChannelKind Kind => NotificationChannelKind.InApp;

    public async Task SendAsync(NotificationDelivery delivery, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var notification = Notification.Create(
            userId: delivery.RecipientUserId,
            type: delivery.Type,
            title: delivery.Content.Title,
            body: delivery.Content.Body,
            link: delivery.Content.Link,
            source: delivery.Source,
            metadata: delivery.Metadata);

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await hub.Clients.Group($"user:{delivery.RecipientUserId}")
            .SendAsync("NotificationCreated", new
            {
                id = notification.Id,
                type = notification.Type,
                title = notification.Title,
                body = notification.Body,
                link = notification.Link,
                source = notification.Source,
                createdAtUtc = notification.CreatedAtUtc,
            }, ct)
            .ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "In-app notification {NotificationId} ({Type}) written for user {UserId}",
                notification.Id, notification.Type, delivery.RecipientUserId);
        }
    }
}
