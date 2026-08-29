using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Scheduling.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Занятие перенесено» → students, guardians and the teacher, in-app + e-mail, immediately.</summary>
public sealed class SessionRescheduledIntegrationEventHandler(
    SchoolNotificationFanout fanout,
    NotificationTimeFormatter time)
    : IIntegrationEventHandler<SessionRescheduledIntegrationEvent>
{
    public async Task HandleAsync(SessionRescheduledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var audience = await fanout.ResolveGroupAsync(@event.StudyGroupId, includeTeacher: true, ct).ConfigureAwait(false);
        if (audience is null)
        {
            return;
        }

        var tokens = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["group"] = audience.GroupName,
            ["oldStart"] = await time.ToSchoolLocalAsync(@event.OldStartUtc, ct).ConfigureAwait(false),
            ["newStart"] = await time.ToSchoolLocalAsync(@event.NewStartUtc, ct).ConfigureAwait(false),
            ["sessionId"] = @event.NewSessionId.ToString(),
        };

        await fanout.DispatchAsync(
            audience.Everyone, NotificationTypes.SessionRescheduled, "Scheduling",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { sessionId = @event.NewSessionId, studyGroupId = @event.StudyGroupId },
            ct).ConfigureAwait(false);
    }
}
