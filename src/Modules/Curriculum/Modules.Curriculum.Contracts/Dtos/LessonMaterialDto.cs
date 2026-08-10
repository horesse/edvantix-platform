namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record LessonMaterialDto(
    Guid Id,
    Guid LessonId,
    MaterialKind Kind,
    string Title,
    Guid? FileId,
    string? Url,
    bool VisibleToStudents,
    int SortOrder);
