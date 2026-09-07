using FSH.Framework.Core.Context;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;
using FSH.Modules.Scheduling.Services;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.IcalSubscription.GetIcalSubscription;

public sealed class GetIcalSubscriptionQueryHandler(
    IIcalSubscriptionService subscriptions,
    ICurrentUser currentUser)
    : IQueryHandler<GetIcalSubscriptionQuery, IcalSubscriptionDto?>
{
    public async ValueTask<IcalSubscriptionDto?> Handle(GetIcalSubscriptionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var row = await subscriptions.GetForCurrentUserAsync(cancellationToken).ConfigureAwait(false);
        if (row is null)
        {
            return null;
        }

        return new IcalSubscriptionDto(row.Token, IcalSubscriptionPath.Build(currentUser.GetTenant(), row.Token));
    }
}
