using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.DeleteCourseModule;

public sealed class DeleteCourseModuleCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<DeleteCourseModuleCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteCourseModuleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var module = await dbContext.CourseModules
            .FirstOrDefaultAsync(m => m.Id == command.CourseModuleId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course module {command.CourseModuleId} not found.");

        // Cascade: lessons under this module, and materials under those lessons, go with it —
        // no restore path exists for any of the three (see docs → "Проектные решения").
        var lessonIds = await dbContext.Lessons
            .Where(l => l.CourseModuleId == module.Id)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (lessonIds.Count > 0)
        {
            var materials = dbContext.LessonMaterials.Where(m => lessonIds.Contains(m.LessonId));
            dbContext.LessonMaterials.RemoveRange(materials);

            var lessons = dbContext.Lessons.Where(l => lessonIds.Contains(l.Id));
            dbContext.Lessons.RemoveRange(lessons);
        }

        dbContext.CourseModules.Remove(module);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
