using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.StudyGroups.Contracts.Events;

public sealed record StudyGroupActivatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudyGroupId)
    : IIntegrationEvent;
