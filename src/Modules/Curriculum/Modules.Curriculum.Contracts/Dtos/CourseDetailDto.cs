namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record CourseDetailDto(
    Guid Id,
    Guid SubjectId,
    string Title,
    string Slug,
    string? Description,
    CourseLevel Level,
    int DurationHours,
    CourseStatus Status,
    Guid? CoverFileId,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CourseModuleWithLessonsDto> Modules);

public sealed record CourseModuleWithLessonsDto(
    Guid Id,
    string Title,
    string? Description,
    int SortOrder,
    IReadOnlyList<LessonDto> Lessons);
