using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Features.v1.Lessons;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.GetCourseById;

public sealed class GetCourseByIdQueryHandler(CurriculumDbContext dbContext)
    : IQueryHandler<GetCourseByIdQuery, CourseDetailDto>
{
    public async ValueTask<CourseDetailDto> Handle(GetCourseByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var course = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {query.CourseId} not found.");

        var modules = await dbContext.CourseModules
            .AsNoTracking()
            .Where(m => m.CourseId == course.Id)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var moduleIds = modules.Select(m => m.Id).ToList();
        var lessons = await dbContext.Lessons
            .AsNoTracking()
            .Where(l => moduleIds.Contains(l.CourseModuleId))
            .OrderBy(l => l.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var lessonsByModule = lessons.ToLookup(l => l.CourseModuleId);

        var moduleDtos = modules
            .Select(m => new CourseModuleWithLessonsDto(
                m.Id,
                m.Title,
                m.Description,
                m.SortOrder,
                lessonsByModule[m.Id].Select(l => l.ToDto()).ToList()))
            .ToList();

        return new CourseDetailDto(
            course.Id,
            course.SubjectId,
            course.Title,
            course.Slug,
            course.Description,
            course.Level,
            course.DurationHours,
            course.Status,
            course.CoverFileId,
            course.PublishedAtUtc,
            course.CreatedAtUtc,
            moduleDtos);
    }
}
