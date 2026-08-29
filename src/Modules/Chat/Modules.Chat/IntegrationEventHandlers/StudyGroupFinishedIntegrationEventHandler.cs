using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Chat.Data;
using FSH.Modules.StudyGroups.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Chat.IntegrationEventHandlers;

/// <summary>
/// Locks the study group's channel when the group finishes: the history stays readable, new
/// messages are rejected (<c>ChatChannel.Lock</c> → <c>SendMessageCommandHandler</c> returns 409).
/// Idempotent.
/// </summary>
public sealed class StudyGroupFinishedIntegrationEventHandler(
    ChatDbContext db,
    ILogger<StudyGroupFinishedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudyGroupFinishedIntegrationEvent>
{
    public async Task HandleAsync(StudyGroupFinishedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var channel = await StudyGroupChannelSync.FindChannelAsync(db, @event.StudyGroupId, ct).ConfigureAwait(false);
        if (channel is null || channel.IsLocked)
        {
            return;
        }

        channel.Lock();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Chat] locked channel {ChannelId} — study group {StudyGroupId} finished", channel.Id, @event.StudyGroupId);
        }
    }
}
