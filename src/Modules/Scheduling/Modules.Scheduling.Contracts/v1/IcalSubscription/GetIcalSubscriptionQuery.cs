using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;

/// <summary>The current user's iCal feed subscription, or <c>null</c> if they've never created one.
/// Gated by <c>Sessions.ViewOwn</c>.</summary>
public sealed record GetIcalSubscriptionQuery : IQuery<IcalSubscriptionDto?>;
