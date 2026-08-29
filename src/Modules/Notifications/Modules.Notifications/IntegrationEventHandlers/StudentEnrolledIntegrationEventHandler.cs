using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.StudyGroups.Contracts;
using FSH.Modules.StudyGroups.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Зачислен в группу» → the student and their guardians, in-app + e-mail.</summary>
public sealed class StudentEnrolledIntegrationEventHandler(
    SchoolNotificationFanout fanout,
    IStudyGroupQueryService studyGroups)
    : IIntegrationEventHandler<StudentEnrolledIntegrationEvent>
{
    public async Task HandleAsync(StudentEnrolledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var brief = await studyGroups.GetBriefAsync(@event.StudyGroupId, ct).ConfigureAwait(false);
        var students = await fanout.ResolveStudentsAsync(@event.StudentId, ct).ConfigureAwait(false);
        if (brief is null || students.Count == 0)
        {
            return;
        }

        var student = students[0];
        var tokens = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["student"] = student.StudentDisplayName,
            ["group"] = brief.Name,
            ["studyGroupId"] = @event.StudyGroupId.ToString(),
        };

        await fanout.DispatchAsync(
            new[] { student.Student }.Concat(student.Guardians),
            NotificationTypes.EnrolledInGroup, "StudyGroups",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { studyGroupId = @event.StudyGroupId, studentId = @event.StudentId },
            ct).ConfigureAwait(false);
    }
}
