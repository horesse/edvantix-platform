using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.StudyGroups.Contracts.Events;

public sealed record StudentUnenrolledIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudyGroupId,
    Guid StudentId,
    DateOnly LeftOn,
    string? Reason)
    : IIntegrationEvent;
