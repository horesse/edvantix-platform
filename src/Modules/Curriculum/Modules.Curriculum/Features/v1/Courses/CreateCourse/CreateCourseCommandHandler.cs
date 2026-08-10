using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.CreateCourse;

public sealed class CreateCourseCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<CreateCourseCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool subjectExists = await dbContext.Subjects
            .AnyAsync(s => s.Id == command.SubjectId, cancellationToken)
            .ConfigureAwait(false);
        if (!subjectExists)
        {
            throw new NotFoundException($"Subject {command.SubjectId} not found.");
        }

        var course = Course.Create(
            command.SubjectId,
            command.Title,
            command.Description,
            command.Level,
            command.DurationHours,
            command.CoverFileId);

        bool slugTaken = await dbContext.Courses
            .AnyAsync(c => c.Slug == course.Slug, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"A course with title '{command.Title}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Courses.Add(course);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return course.Id;
    }
}
