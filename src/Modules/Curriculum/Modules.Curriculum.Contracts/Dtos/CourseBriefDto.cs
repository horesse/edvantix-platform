namespace FSH.Modules.Curriculum.Contracts.Dtos;

/// <summary>Minimal course projection for <c>ICourseQueryService.GetBriefAsync</c> — used by
/// StudyGroups/Payments to show a course title without pulling in the full <see cref="CourseDto"/>.</summary>
public sealed record CourseBriefDto(
    Guid Id,
    string Title,
    CourseStatus Status);
