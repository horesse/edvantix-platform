using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.IcalSubscription;

/// <summary>Anonymous iCal feed for a personal schedule. The caller is identified by
/// <paramref name="Token"/> (a personal <c>IcalSubscriptionToken</c>); the tenant is carried in the
/// request's <c>?tenant=</c> and resolved before this runs. Returns a VCALENDAR body, or
/// <c>null</c> when the token is unknown (→ 404, so a wrong link can't distinguish "bad token" from
/// "empty schedule"). <paramref name="From"/>/<paramref name="To"/> default to a rolling
/// [-7 days, +60 days] window.</summary>
public sealed record GetScheduleIcsQuery(string Token, DateTimeOffset? From, DateTimeOffset? To)
    : IQuery<string?>;
