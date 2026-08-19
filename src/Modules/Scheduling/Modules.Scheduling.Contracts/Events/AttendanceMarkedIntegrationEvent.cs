using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Scheduling.Contracts.Dtos;

namespace FSH.Modules.Scheduling.Contracts.Events;

public sealed record AttendanceMarkedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SessionId,
    Guid StudentId,
    AttendanceStatus Status)
    : IIntegrationEvent;
