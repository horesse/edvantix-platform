using FSH.Framework.Core.Context;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using FSH.Modules.Scheduling.Services;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.RotateIcalSubscription;

public sealed class RotateIcalSubscriptionCommandHandler(
    IIcalSubscriptionService subscriptions,
    ICurrentUser currentUser)
    : ICommandHandler<RotateIcalSubscriptionCommand, IcalSubscriptionDto>
{
    public async ValueTask<IcalSubscriptionDto> Handle(RotateIcalSubscriptionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var row = await subscriptions.RotateForCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        return new IcalSubscriptionDto(row.Token, IcalSubscriptionPath.Build(currentUser.GetTenant(), row.Token));
    }
}
