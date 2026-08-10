namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record LessonDto(
    Guid Id,
    Guid CourseModuleId,
    string Title,
    string? Objectives,
    string? Content,
    int DurationMinutes,
    int SortOrder);
