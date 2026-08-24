using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Payments.Services;
using FSH.Modules.Scheduling.Contracts.Events;

namespace FSH.Modules.Payments.IntegrationEventHandlers;

/// <summary>"Накопление для потарифного начисления" (see docs/02 Модули/Payments.md → «Подписки») —
/// PerLesson/PerMonth accrual is computed live from Scheduling at generation time (via
/// <c>ISessionPlanQueryService</c>/<c>IAttendanceQueryService</c>), so there's no running counter
/// here to increment. What a newly-held session <em>does</em> change is any already-generated but
/// still-<c>Draft</c> invoice for the group — <see cref="IDraftInvoiceRefreshService"/> recomputes
/// those so they don't go stale before being issued.</summary>
public sealed class SessionHeldIntegrationEventHandler(IDraftInvoiceRefreshService refreshService)
    : IIntegrationEventHandler<SessionHeldIntegrationEvent>
{
    public Task HandleAsync(SessionHeldIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return refreshService.RefreshForGroupAsync(@event.StudyGroupId, ct);
    }
}
