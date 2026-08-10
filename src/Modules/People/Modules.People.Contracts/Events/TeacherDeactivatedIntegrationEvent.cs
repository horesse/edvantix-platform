using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.People.Contracts.Events;

public sealed record TeacherDeactivatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid TeacherId)
    : IIntegrationEvent;
