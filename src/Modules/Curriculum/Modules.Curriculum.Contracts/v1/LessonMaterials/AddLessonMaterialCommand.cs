using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

/// <summary>Exactly one of <paramref name="FileId"/>/<paramref name="Url"/> must be set —
/// enforced by <c>AddLessonMaterialCommandValidator</c>, the domain, and a DB CHECK constraint.</summary>
public sealed record AddLessonMaterialCommand(
    Guid LessonId,
    MaterialKind Kind,
    string Title,
    Guid? FileId,
    string? Url,
    bool VisibleToStudents) : ICommand<LessonMaterialDto>;
