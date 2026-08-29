using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Chat.Data;
using FSH.Modules.People.Contracts;
using FSH.Modules.StudyGroups.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Chat.IntegrationEventHandlers;

/// <summary>
/// Adds the enrolled student to the study group's channel. When the student has no account the
/// primary-payer guardian stands in (see <see cref="StudyGroupChannelSync.ResolveChatUserId"/>);
/// when nobody in the family has an account, nothing is added and the handler is a no-op.
/// </summary>
public sealed class StudentEnrolledIntegrationEventHandler(
    ChatDbContext db,
    IPeopleLookupService people,
    ILogger<StudentEnrolledIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentEnrolledIntegrationEvent>
{
    public async Task HandleAsync(StudentEnrolledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var channel = await StudyGroupChannelSync.FindChannelAsync(db, @event.StudyGroupId, ct).ConfigureAwait(false);
        if (channel is null)
        {
            return;
        }

        var contacts = await people.GetStudentContactsAsync([@event.StudentId], ct).ConfigureAwait(false);
        var userId = contacts.Count == 0 ? null : StudyGroupChannelSync.ResolveChatUserId(contacts[0]);
        if (userId is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "[Chat] student {StudentId} enrolled in {StudyGroupId} has no chat-capable account; not added to channel {ChannelId}",
                    @event.StudentId, @event.StudyGroupId, channel.Id);
            }

            return;
        }

        if (channel.HasMember(userId))
        {
            return;
        }

        channel.AddMember(userId, StudyGroupChannelSync.SystemActor);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
