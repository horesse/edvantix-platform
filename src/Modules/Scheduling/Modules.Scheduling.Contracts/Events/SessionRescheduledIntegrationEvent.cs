using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Scheduling.Contracts.Events;

public sealed record SessionRescheduledIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SessionId,
    Guid NewSessionId,
    Guid StudyGroupId,
    DateTimeOffset OldStartUtc,
    DateTimeOffset NewStartUtc)
    : IIntegrationEvent;
