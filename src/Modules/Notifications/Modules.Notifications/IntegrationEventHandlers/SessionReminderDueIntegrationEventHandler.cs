using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Scheduling.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Занятие завтра» → students and guardians, in-app + e-mail (fired ~24h ahead by Scheduling's job).</summary>
public sealed class SessionReminderDueIntegrationEventHandler(
    SchoolNotificationFanout fanout,
    NotificationTimeFormatter time)
    : IIntegrationEventHandler<SessionReminderDueIntegrationEvent>
{
    public async Task HandleAsync(SessionReminderDueIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var audience = await fanout.ResolveGroupAsync(@event.StudyGroupId, includeTeacher: false, ct).ConfigureAwait(false);
        if (audience is null)
        {
            return;
        }

        var tokens = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["group"] = audience.GroupName,
            ["start"] = await time.ToSchoolLocalAsync(@event.StartUtc, ct).ConfigureAwait(false),
            ["sessionId"] = @event.SessionId.ToString(),
        };

        await fanout.DispatchAsync(
            audience.StudentsAndGuardians, NotificationTypes.SessionReminder, "Scheduling",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { sessionId = @event.SessionId, studyGroupId = @event.StudyGroupId },
            ct).ConfigureAwait(false);
    }
}
