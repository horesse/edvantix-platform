using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.People.Contracts.Dtos;

namespace FSH.Modules.People.Contracts.Events;

public sealed record StudentStatusChangedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudentId,
    StudentStatus From,
    StudentStatus To)
    : IIntegrationEvent;
