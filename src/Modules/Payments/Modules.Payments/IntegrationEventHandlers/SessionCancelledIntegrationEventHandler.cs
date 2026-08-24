using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Payments.Services;
using FSH.Modules.Scheduling.Contracts.Events;

namespace FSH.Modules.Payments.IntegrationEventHandlers;

/// <summary>"Исключить из начислений" (see docs/02 Модули/Payments.md → «Подписки») — a cancelled
/// session never produced attendance rows in the first place, so <c>IAttendanceQueryService</c>
/// already excludes it from any <em>new</em> accrual calculation. The remaining concern is an
/// already-generated <c>Draft</c> invoice whose PerLesson/PerMonth line was computed before this
/// cancellation — <see cref="IDraftInvoiceRefreshService"/> recomputes it.</summary>
public sealed class SessionCancelledIntegrationEventHandler(IDraftInvoiceRefreshService refreshService)
    : IIntegrationEventHandler<SessionCancelledIntegrationEvent>
{
    public Task HandleAsync(SessionCancelledIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return refreshService.RefreshForGroupAsync(@event.StudyGroupId, ct);
    }
}
