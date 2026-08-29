using FSH.Framework.Core.Domain;

namespace FSH.Modules.Notifications.Domain;

/// <summary>
/// A digestable e-mail notification held back for aggregation: 10 cancelled lessons become one
/// summary e-mail instead of ten. Written instead of sending immediately; a recurring job flushes
/// each recipient's batch once its oldest entry crosses the aggregation window. Tenant-isolated.
/// </summary>
public sealed class PendingNotificationDigest : AggregateRoot<Guid>
{
    public string RecipientUserId { get; private set; } = default!;
    public string RecipientEmail { get; private set; } = default!;

    /// <summary>A <c>NotificationTypes</c> key.</summary>
    public string Type { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public string? Link { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    private PendingNotificationDigest() { }

    public static PendingNotificationDigest Create(
        string recipientUserId, string recipientEmail, string type, string title, string? body, string? link)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new PendingNotificationDigest
        {
            Id = Guid.CreateVersion7(),
            RecipientUserId = recipientUserId,
            RecipientEmail = recipientEmail,
            Type = type,
            Title = title,
            Body = body,
            Link = link,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkSent(DateTime sentAtUtc) => SentAtUtc = sentAtUtc;
}
