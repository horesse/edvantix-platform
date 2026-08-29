using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>
/// «Пропуск без уважительной причины» → the student's guardians, in-app + e-mail. Only fires for
/// <see cref="AttendanceStatus.Absent"/> (<c>Excused</c> / <c>Late</c> / <c>Present</c> are silent).
/// </summary>
public sealed class AttendanceMarkedIntegrationEventHandler(SchoolNotificationFanout fanout)
    : IIntegrationEventHandler<AttendanceMarkedIntegrationEvent>
{
    public async Task HandleAsync(AttendanceMarkedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event.Status != AttendanceStatus.Absent)
        {
            return;
        }

        var students = await fanout.ResolveStudentsAsync(@event.StudentId, ct).ConfigureAwait(false);
        if (students.Count == 0)
        {
            return;
        }

        var student = students[0];
        var tokens = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["student"] = student.StudentDisplayName,
            ["studentId"] = @event.StudentId.ToString(),
        };

        await fanout.DispatchAsync(
            student.Guardians, NotificationTypes.AttendanceUnexcused, "Scheduling",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { sessionId = @event.SessionId, studentId = @event.StudentId },
            ct).ConfigureAwait(false);
    }
}
