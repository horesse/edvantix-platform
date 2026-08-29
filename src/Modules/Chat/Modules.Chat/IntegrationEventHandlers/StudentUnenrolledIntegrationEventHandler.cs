using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Chat.Data;
using FSH.Modules.People.Contracts;
using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Events;

namespace FSH.Modules.Chat.IntegrationEventHandlers;

/// <summary>
/// Removes the unenrolled student from the study group's channel — unless the account that
/// represented them still represents another active student in the same group. That happens when a
/// guardian pays for two children in one group and the student has no login of their own: dropping
/// the guardian would cut them off from the sibling's channel too.
/// </summary>
public sealed class StudentUnenrolledIntegrationEventHandler(
    ChatDbContext db,
    IPeopleLookupService people,
    IStudyGroupQueryService studyGroups)
    : IIntegrationEventHandler<StudentUnenrolledIntegrationEvent>
{
    public async Task HandleAsync(StudentUnenrolledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var channel = await StudyGroupChannelSync.FindChannelAsync(db, @event.StudyGroupId, ct).ConfigureAwait(false);
        if (channel is null)
        {
            return;
        }

        var leaving = await people.GetStudentContactsAsync([@event.StudentId], ct).ConfigureAwait(false);
        var leavingUserId = leaving.Count == 0 ? null : StudyGroupChannelSync.ResolveChatUserId(leaving[0]);
        if (leavingUserId is null || !channel.HasMember(leavingUserId))
        {
            return;
        }

        // Still needed by anyone else active in the group?
        var stillActive = await studyGroups
            .GetActiveStudentIdsAsync(@event.StudyGroupId, DateOnly.FromDateTime(DateTime.UtcNow), ct)
            .ConfigureAwait(false);
        var remaining = stillActive.Where(id => id != @event.StudentId).ToArray();
        if (remaining.Length > 0)
        {
            var remainingContacts = await people.GetStudentContactsAsync(remaining, ct).ConfigureAwait(false);
            if (remainingContacts.Any(c => StudyGroupChannelSync.ResolveChatUserId(c) == leavingUserId))
            {
                return;
            }
        }

        channel.RemoveMember(leavingUserId, StudyGroupChannelSync.SystemActor);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
