using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.UpdateLesson;

public sealed class UpdateLessonCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<UpdateLessonCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateLessonCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var lesson = await dbContext.Lessons
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Lesson {command.LessonId} not found.");

        lesson.Update(command.Title, command.Objectives, command.Content, command.DurationMinutes);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
