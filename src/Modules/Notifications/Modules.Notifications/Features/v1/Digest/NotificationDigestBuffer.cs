using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using FSH.Modules.Notifications.Templating;

namespace FSH.Modules.Notifications.Features.v1.Digest;

/// <summary>Holds a digestable e-mail notification for later aggregation by <see cref="NotificationDigestJob"/>.</summary>
public interface INotificationDigestBuffer
{
    Task EnqueueAsync(
        string recipientUserId, string recipientEmail, string type, RenderedNotification content, CancellationToken ct = default);
}

public sealed class NotificationDigestBuffer(NotificationsDbContext db) : INotificationDigestBuffer
{
    public async Task EnqueueAsync(
        string recipientUserId, string recipientEmail, string type, RenderedNotification content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        db.PendingNotificationDigests.Add(PendingNotificationDigest.Create(
            recipientUserId, recipientEmail, type, content.Title, content.Body, content.Link));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
