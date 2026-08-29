using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Scheduling.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Занятие отменено» → students, guardians and the teacher, in-app + e-mail, immediately.</summary>
public sealed class SessionCancelledIntegrationEventHandler(SchoolNotificationFanout fanout)
    : IIntegrationEventHandler<SessionCancelledIntegrationEvent>
{
    public async Task HandleAsync(SessionCancelledIntegrationEvent @event, CancellationToken ct = default)
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
            ["reason"] = string.IsNullOrWhiteSpace(@event.Reason) ? string.Empty : $"Reason: {@event.Reason}",
            ["sessionId"] = @event.SessionId.ToString(),
        };

        await fanout.DispatchAsync(
            audience.Everyone, NotificationTypes.SessionCancelled, "Scheduling",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { sessionId = @event.SessionId, studyGroupId = @event.StudyGroupId },
            ct).ConfigureAwait(false);
    }
}
