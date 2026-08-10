using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.People.Contracts.Events;

public sealed record StudentCreatedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudentId,
    string LastName,
    string FirstName)
    : IIntegrationEvent;
