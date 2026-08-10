using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.DeleteCourse;

public sealed class DeleteCourseCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<DeleteCourseCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {command.CourseId} not found.");

        dbContext.Courses.Remove(course);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
