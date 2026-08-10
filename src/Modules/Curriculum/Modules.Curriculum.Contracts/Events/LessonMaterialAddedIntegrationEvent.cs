using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Curriculum.Contracts.Dtos;

namespace FSH.Modules.Curriculum.Contracts.Events;

public sealed record LessonMaterialAddedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid LessonId,
    Guid MaterialId,
    MaterialKind Kind) : IIntegrationEvent;
