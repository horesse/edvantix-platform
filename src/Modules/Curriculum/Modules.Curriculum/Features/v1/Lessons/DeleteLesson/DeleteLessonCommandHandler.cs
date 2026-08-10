using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.DeleteLesson;

public sealed class DeleteLessonCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<DeleteLessonCommand, Unit>
{
    // NOTE(Scheduling, not yet built): once Session.LessonId exists, block deletion when held
    // sessions reference this lesson (docs/02 Модули/Curriculum.md → Инварианты — "только
    // архивация"). Curriculum has no concept of "archived lesson" today; revisit then.
    public async ValueTask<Unit> Handle(DeleteLessonCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lesson = await dbContext.Lessons
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Lesson {command.LessonId} not found.");

        var materials = dbContext.LessonMaterials.Where(m => m.LessonId == lesson.Id);
        dbContext.LessonMaterials.RemoveRange(materials);

        dbContext.Lessons.Remove(lesson);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
