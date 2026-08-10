using FSH.Modules.Curriculum.Contracts.Dtos;

namespace FSH.Modules.Curriculum.Contracts;

/// <summary>
/// Synchronous read access to courses/lessons for other modules — StudyGroups checks
/// <see cref="IsPublishedAsync"/> before letting a group be created against a course;
/// Scheduling calls <see cref="GetLessonsInOrderAsync"/> to attach generated sessions to
/// program lessons (see docs/05 Решения (ADR)/ADR-006).
/// </summary>
public interface ICourseQueryService
{
    ValueTask<CourseBriefDto?> GetBriefAsync(Guid courseId, CancellationToken cancellationToken = default);

    ValueTask<bool> IsPublishedAsync(Guid courseId, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<LessonBriefDto>> GetLessonsInOrderAsync(
        Guid courseId, CancellationToken cancellationToken = default);
}
