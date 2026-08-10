namespace FSH.Modules.Curriculum.Contracts.Dtos;

/// <summary>Minimal lesson projection for <c>ICourseQueryService.GetLessonsInOrderAsync</c> —
/// consumed by Scheduling to attach generated sessions to a program lesson.</summary>
public sealed record LessonBriefDto(
    Guid Id,
    Guid CourseModuleId,
    string Title,
    int SortOrder);
