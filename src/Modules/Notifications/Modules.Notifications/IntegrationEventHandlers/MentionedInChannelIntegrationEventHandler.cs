using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// Subscribes to <see cref="MentionedInChannelIntegrationEvent"/> emitted by the Chat module's
/// <c>SendMessageCommandHandler</c> and raises an in-app notification for the mentioned user via
/// <see cref="INotificationDispatcher"/> (inbox row + live SignalR push).
///
/// Runs in the publisher's scope (in-memory bus, synchronous dispatch), so the work is minimal and
/// any exception surfaces to the SendMessage request. Chat mentions are in-app only.
/// </summary>
public sealed class MentionedInChannelIntegrationEventHandler(INotificationDispatcher dispatcher)
    : IIntegrationEventHandler<MentionedInChannelIntegrationEvent>
{
    public async Task HandleAsync(MentionedInChannelIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var channel = string.IsNullOrEmpty(@event.ChannelName)
            ? "a conversation"
            : $"#{@event.ChannelName}";

        await dispatcher.DispatchAsync(
            new NotificationRequest(
                RecipientUserId: @event.MentionedUserId,
                TemplateKey: NotificationTypes.ChatMention,
                Tokens: new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["channel"] = channel,
                    ["preview"] = @event.BodyPreview,
                    ["channelId"] = @event.ChannelId.ToString(),
                    ["messageId"] = @event.MessageId.ToString(),
                })
            {
                Source = @event.Source,
                Channels = NotificationChannelKind.InApp,
                PreferenceUserId = @event.MentionedUserId,
                ExpectedTenantId = @event.TenantId,
                Metadata = new
                {
                    channelId = @event.ChannelId,
                    channelName = @event.ChannelName,
                    messageId = @event.MessageId,
                    authorUserId = @event.AuthorUserId,
                },
            },
            ct).ConfigureAwait(false);
    }
}
