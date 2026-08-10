using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Curriculum.Contracts.Events;

public sealed record CourseArchivedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid CourseId) : IIntegrationEvent;
