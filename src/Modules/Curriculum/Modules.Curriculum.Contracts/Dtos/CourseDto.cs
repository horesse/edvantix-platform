namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record CourseDto(
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
    DateTimeOffset CreatedAtUtc);
