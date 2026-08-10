using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.RestoreCourse;

public sealed class RestoreCourseCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<RestoreCourseCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreCourseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var course = await dbContext.Courses
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(c => c.Id == command.CourseId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course {command.CourseId} not found.");

        course.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return course.Id;
    }
}
