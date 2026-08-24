namespace FSH.Modules.StudyGroups.Contracts.Dtos;

/// <summary>Minimal projection for <c>IStudyGroupQueryService.GetBriefAsync</c> — used by
/// Scheduling/Payments to show a group name without pulling in the full <see cref="StudyGroupDto"/>.
/// <paramref name="PrimaryTeacherId"/> is the fallback teacher for a generated
/// <c>Scheduling.Session</c> when its <c>ScheduleTemplate.TeacherId</c> override is null — see
/// docs/02 Модули/Scheduling.md → "Генерация".</summary>
public sealed record StudyGroupBriefDto(
    Guid Id,
    string Code,
    string Name,
    Guid CourseId,
    Guid PrimaryTeacherId,
    StudyGroupStatus Status);
