using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.StudyGroups.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.StudyGroups.IntegrationEventHandlers;

/// <summary>
/// Stores the chat channel id Chat provisioned for a group (<c>StudyGroup.ChatChannelId</c>).
/// Closes the loop opened by <c>StudyGroupCreatedIntegrationEvent</c> — Chat publishes this back
/// because it cannot reference the StudyGroups runtime. Idempotent.
/// </summary>
public sealed class StudyGroupChannelLinkedIntegrationEventHandler(
    StudyGroupsDbContext dbContext,
    ILogger<StudyGroupChannelLinkedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudyGroupChannelLinkedIntegrationEvent>
{
    public async Task HandleAsync(StudyGroupChannelLinkedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var group = await dbContext.StudyGroups
            .FirstOrDefaultAsync(g => g.Id == @event.StudyGroupId, ct)
            .ConfigureAwait(false);
        if (group is null || group.ChatChannelId == @event.ChannelId)
        {
            return;
        }

        group.SetChatChannel(@event.ChannelId);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[StudyGroups] linked chat channel {ChannelId} to study group {StudyGroupId}",
                @event.ChannelId, @event.StudyGroupId);
        }
    }
}
