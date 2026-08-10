using FSH.Modules.Curriculum.Contracts;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Services;

/// <summary>
/// Read-only cross-module access to courses/lessons. No caching (unlike
/// <c>IPeopleScopeResolver</c>) — StudyGroups/Scheduling call these per-command, not per-request
/// in a hot list, so the extra round-trip hasn't shown up as a problem; revisit if profiling
/// says otherwise once StudyGroups exists.
/// </summary>
public sealed class CourseQueryService(CurriculumDbContext dbContext) : ICourseQueryService
{
    public async ValueTask<CourseBriefDto?> GetBriefAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken)
            .ConfigureAwait(false);

        return course is null ? null : new CourseBriefDto(course.Id, course.Title, course.Status);
    }

    public async ValueTask<bool> IsPublishedAsync(Guid courseId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId && c.Status == CourseStatus.Published, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<IReadOnlyList<LessonBriefDto>> GetLessonsInOrderAsync(
        Guid courseId, CancellationToken cancellationToken = default)
    {
        var moduleIds = await dbContext.CourseModules
            .AsNoTracking()
            .Where(m => m.CourseId == courseId)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (moduleIds.Count == 0)
        {
            return [];
        }

        var lessons = await dbContext.Lessons
            .AsNoTracking()
            .Where(l => moduleIds.Contains(l.CourseModuleId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Order by (module position, lesson position within module) — a single OrderBy on
        // Lesson.SortOrder alone would interleave lessons from different modules.
        var moduleOrder = moduleIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);

        return lessons
            .OrderBy(l => moduleOrder[l.CourseModuleId])
            .ThenBy(l => l.SortOrder)
            .Select(l => new LessonBriefDto(l.Id, l.CourseModuleId, l.Title, l.SortOrder))
            .ToList();
    }
}
