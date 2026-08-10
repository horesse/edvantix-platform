using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.UpdateCourse;

public sealed class UpdateCourseCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<UpdateCourseCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await dbContext.Courses
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {command.CourseId} not found.");

        bool subjectExists = await dbContext.Subjects
            .AnyAsync(s => s.Id == command.SubjectId, cancellationToken)
            .ConfigureAwait(false);
        if (!subjectExists)
        {
            throw new NotFoundException($"Subject {command.SubjectId} not found.");
        }

        course.Update(
            command.SubjectId,
            command.Title,
            command.Description,
            command.Level,
            command.DurationHours,
            command.CoverFileId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
