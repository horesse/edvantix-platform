using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.People.Contracts.Events;

public sealed record StudentArchivedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid StudentId,
    DateTimeOffset ArchivedOn)
    : IIntegrationEvent;
