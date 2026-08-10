using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.DuplicateCourse;

/// <summary>
/// Deep-clones a course: itself (as a new Draft), then every module → lesson → material, each
/// with a fresh Id but the same SortOrder — see docs/04 Задачи/Задачи · Новые модули.md →
/// Curriculum → "Проектные решения" for why this exists (schools iterate a course from cohort
/// to cohort and don't want to edit one that already has sessions/enrollments against it).
/// </summary>
public sealed class DuplicateCourseCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<DuplicateCourseCommand, Guid>
{
    public async ValueTask<Guid> Handle(DuplicateCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var source = await dbContext.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {command.CourseId} not found.");

        // Two duplicates of the same course would otherwise collide on the unique slug index —
        // a short Guid suffix keeps the copy's slug unique without a user-facing rename step.
        var copy = Course.Create(
            source.SubjectId,
            $"{source.Title} (копия {Guid.NewGuid().ToString()[..8]})",
            source.Description,
            source.Level,
            source.DurationHours,
            source.CoverFileId);
        dbContext.Courses.Add(copy);

        var modules = await dbContext.CourseModules
            .AsNoTracking()
            .Where(m => m.CourseId == source.Id)
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

        var lessonIds = lessons.Select(l => l.Id).ToList();
        var materials = await dbContext.LessonMaterials
            .AsNoTracking()
            .Where(m => lessonIds.Contains(m.LessonId))
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var module in modules)
        {
            var moduleCopy = CourseModule.Create(copy.Id, module.Title, module.Description, module.SortOrder);
            dbContext.CourseModules.Add(moduleCopy);

            foreach (var lesson in lessons.Where(l => l.CourseModuleId == module.Id))
            {
                var lessonCopy = Lesson.Create(
                    moduleCopy.Id, lesson.Title, lesson.Objectives, lesson.Content,
                    lesson.DurationMinutes, lesson.SortOrder);
                dbContext.Lessons.Add(lessonCopy);

                foreach (var material in materials.Where(m => m.LessonId == lesson.Id))
                {
                    var materialCopy = LessonMaterial.Create(
                        lessonCopy.Id, material.Kind, material.Title, material.FileId, material.Url,
                        material.VisibleToStudents, material.SortOrder);
                    dbContext.LessonMaterials.Add(materialCopy);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return copy.Id;
    }
}
