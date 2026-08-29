using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.People.Contracts;
using FSH.Modules.StudyGroups.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Chat.IntegrationEventHandlers;

/// <summary>
/// Provisions the private channel for a new study group and publishes
/// <see cref="StudyGroupChannelLinkedIntegrationEvent"/> so StudyGroups can store the id. The
/// primary teacher (if they have an account) seeds the membership; students are added later on
/// enrolment. Idempotent: a re-delivery finds the existing channel and just re-publishes the link.
/// </summary>
public sealed class StudyGroupCreatedIntegrationEventHandler(
    ChatDbContext db,
    IPeopleLookupService people,
    IEventBus eventBus,
    ILogger<StudyGroupCreatedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudyGroupCreatedIntegrationEvent>
{
    public async Task HandleAsync(StudyGroupCreatedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var existing = await StudyGroupChannelSync.FindChannelAsync(db, @event.StudyGroupId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            await PublishLinkAsync(@event, existing.Id, ct).ConfigureAwait(false);
            return;
        }

        var teacher = await people.GetTeacherContactAsync(@event.PrimaryTeacherId, ct).ConfigureAwait(false);
        var teacherUserId = teacher?.UserId;

        var channel = ChatChannel.CreateForStudyGroup(
            @event.StudyGroupId,
            @event.Name,
            creatorUserId: teacherUserId ?? StudyGroupChannelSync.SystemActor,
            memberUserIds: teacherUserId is null ? [] : [teacherUserId]);

        db.Channels.Add(channel);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await PublishLinkAsync(@event, channel.Id, ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Chat] provisioned channel {ChannelId} for study group {StudyGroupId} (teacher account: {HasTeacher})",
                channel.Id, @event.StudyGroupId, teacherUserId is not null);
        }
    }

    private Task PublishLinkAsync(StudyGroupCreatedIntegrationEvent source, Guid channelId, CancellationToken ct) =>
        eventBus.PublishAsync(
            new StudyGroupChannelLinkedIntegrationEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                source.TenantId,
                source.CorrelationId,
                "Chat",
                source.StudyGroupId,
                channelId),
            ct);
}
