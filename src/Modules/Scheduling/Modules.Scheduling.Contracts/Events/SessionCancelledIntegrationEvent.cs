using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Scheduling.Contracts.Events;

public sealed record SessionCancelledIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SessionId,
    Guid StudyGroupId,
    string? Reason)
    : IIntegrationEvent;
