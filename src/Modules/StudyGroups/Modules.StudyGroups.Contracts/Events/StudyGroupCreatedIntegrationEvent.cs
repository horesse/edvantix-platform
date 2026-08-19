using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.StudyGroups.Contracts.Events;

public sealed record StudyGroupCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudyGroupId,
    string Name,
    Guid CourseId,
    Guid PrimaryTeacherId)
    : IIntegrationEvent;
