using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;

/// <summary>Create the current user's iCal feed subscription, or — if one exists — replace its
/// secret so previously shared links stop working. Gated by <c>Sessions.ViewOwn</c>.</summary>
public sealed record RotateIcalSubscriptionCommand : ICommand<IcalSubscriptionDto>;
