using System.Collections.ObjectModel;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.Channels;

/// <summary>
/// Sends the notification by e-mail via <c>BuildingBlocks/Mailing</c>. No-ops silently when the
/// template has no e-mail body or the recipient has no address. Best-effort: a send failure is
/// logged, never thrown (it must not fail the create/scan that raised the event).
/// </summary>
public sealed class EmailNotificationChannel(
    IMailService mailService,
    ILogger<EmailNotificationChannel> logger)
    : INotificationChannel
{
    public NotificationChannelKind Kind => NotificationChannelKind.Email;

    public async Task SendAsync(NotificationDelivery delivery, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        if (!delivery.Content.HasEmail || string.IsNullOrWhiteSpace(delivery.RecipientEmail))
        {
            return;
        }

        try
        {
            await mailService.SendAsync(
                new MailRequest(
                    to: new Collection<string> { delivery.RecipientEmail },
                    subject: delivery.Content.EmailSubject!,
                    body: delivery.Content.EmailHtmlBody!),
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex, "Failed to e-mail {Type} notification to {Email}", delivery.Type, delivery.RecipientEmail);
        }
    }
}
