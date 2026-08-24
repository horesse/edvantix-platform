using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.StudyGroups.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Payments.IntegrationEventHandlers;

/// <summary>"Начать тарификацию" (see docs/02 Модули/Payments.md → «Подписки») — deliberately a
/// no-op. Payments has no per-enrollment state to create up front: <c>BulkGenerateInvoicesCommand</c>
/// resolves the active roster live from <c>IStudyGroupQueryService</c> every time it runs, so a newly
/// enrolled student is picked up automatically on the next generation for that group/period — there
/// is nothing to seed. Kept as a real, registered handler (not just absent from the subscription
/// list) so the Inbox dedup + logging story stays consistent if a future feature (e.g. a welcome
/// notification, or eager per-lesson accrual bookkeeping) needs to hook this event.</summary>
public sealed class StudentEnrolledIntegrationEventHandler(ILogger<StudentEnrolledIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentEnrolledIntegrationEvent>
{
    public Task HandleAsync(StudentEnrolledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "[Payments] StudentEnrolled noted for student {StudentId} in group {StudyGroupId} — no action needed, accrual is resolved live",
                @event.StudentId, @event.StudyGroupId);
        }

        return Task.CompletedTask;
    }
}
