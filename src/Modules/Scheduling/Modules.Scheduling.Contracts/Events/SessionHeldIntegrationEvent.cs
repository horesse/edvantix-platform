using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Scheduling.Contracts.Events;

public sealed record SessionHeldIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SessionId,
    Guid StudyGroupId,
    Guid? LessonId,
    DateTime HeldAtUtc)
    : IIntegrationEvent;
