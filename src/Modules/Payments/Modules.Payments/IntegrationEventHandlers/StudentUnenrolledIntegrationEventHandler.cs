using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Payments.Services;
using FSH.Modules.StudyGroups.Contracts.Events;

namespace FSH.Modules.Payments.IntegrationEventHandlers;

/// <summary>"Остановить тарификацию" (see docs/02 Модули/Payments.md → «Подписки») —
/// <c>BulkGenerateInvoicesCommand</c> already excludes non-active enrollments going forward, so
/// there's nothing to stop for <em>future</em> runs. What needs handling is a <c>Draft</c> invoice
/// generated <em>before</em> this student left — <see cref="IDraftInvoiceRefreshService"/> clears its
/// per-tariff line rather than leaving a stale amount (see that service's remarks on why it clears
/// instead of guessing a prorated figure).</summary>
public sealed class StudentUnenrolledIntegrationEventHandler(IDraftInvoiceRefreshService refreshService)
    : IIntegrationEventHandler<StudentUnenrolledIntegrationEvent>
{
    public Task HandleAsync(StudentUnenrolledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return refreshService.RefreshForGroupAsync(@event.StudyGroupId, ct);
    }
}
