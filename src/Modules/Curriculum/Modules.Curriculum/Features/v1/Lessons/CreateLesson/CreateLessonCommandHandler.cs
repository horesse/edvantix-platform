using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.CreateLesson;

public sealed class CreateLessonCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<CreateLessonCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateLessonCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool moduleExists = await dbContext.CourseModules
            .AnyAsync(m => m.Id == command.CourseModuleId, cancellationToken)
            .ConfigureAwait(false);
        if (!moduleExists)
        {
            throw new NotFoundException($"Course module {command.CourseModuleId} not found.");
        }

        int nextOrder = await dbContext.Lessons
            .Where(l => l.CourseModuleId == command.CourseModuleId)
            .Select(l => (int?)l.SortOrder)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false) is { } max ? max + 1 : 0;

        var lesson = Lesson.Create(
            command.CourseModuleId, command.Title, command.Objectives, command.Content,
            command.DurationMinutes, nextOrder);

        dbContext.Lessons.Add(lesson);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return lesson.Id;
    }
}
