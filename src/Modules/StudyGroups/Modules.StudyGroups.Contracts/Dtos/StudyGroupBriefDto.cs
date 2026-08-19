namespace FSH.Modules.StudyGroups.Contracts.Dtos;

/// <summary>Minimal projection for <c>IStudyGroupQueryService.GetBriefAsync</c> — used by
/// Scheduling/Payments to show a group name without pulling in the full <see cref="StudyGroupDto"/>.</summary>
public sealed record StudyGroupBriefDto(
    Guid Id,
    string Code,
    string Name,
    Guid CourseId,
    StudyGroupStatus Status);
