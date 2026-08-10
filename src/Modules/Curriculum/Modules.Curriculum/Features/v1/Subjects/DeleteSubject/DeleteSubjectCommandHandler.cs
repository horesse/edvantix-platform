using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.DeleteSubject;

public sealed class DeleteSubjectCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<DeleteSubjectCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSubjectCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subject = await dbContext.Subjects
            .FirstOrDefaultAsync(s => s.Id == command.SubjectId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Subject {command.SubjectId} not found.");

        bool hasChildren = await dbContext.Subjects
            .AnyAsync(s => s.ParentId == subject.Id, cancellationToken)
            .ConfigureAwait(false);
        if (hasChildren)
        {
            throw new CustomException(
                "Cannot delete a subject that has child subjects. Move or remove the children first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        bool hasCourses = await dbContext.Courses
            .AnyAsync(c => c.SubjectId == subject.Id, cancellationToken)
            .ConfigureAwait(false);
        if (hasCourses)
        {
            throw new CustomException(
                "Cannot delete a subject that has courses. Move or remove the courses first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        dbContext.Subjects.Remove(subject);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
