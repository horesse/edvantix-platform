using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.StudyGroups.Contracts.Events;

public sealed record StudentEnrolledIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudyGroupId,
    Guid StudentId,
    DateOnly EnrolledOn,
    Guid? TariffId)
    : IIntegrationEvent;
