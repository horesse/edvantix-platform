using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;

/// <summary>Delete the current user's iCal feed subscription — every copied link stops working.
/// Idempotent. Gated by <c>Sessions.ViewOwn</c>.</summary>
public sealed record RevokeIcalSubscriptionCommand : ICommand<Unit>;
