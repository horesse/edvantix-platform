using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using FSH.Modules.Scheduling.Services;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.RevokeIcalSubscription;

public sealed class RevokeIcalSubscriptionCommandHandler(IIcalSubscriptionService subscriptions)
    : ICommandHandler<RevokeIcalSubscriptionCommand, Unit>
{
    public async ValueTask<Unit> Handle(RevokeIcalSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await subscriptions.RevokeForCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
