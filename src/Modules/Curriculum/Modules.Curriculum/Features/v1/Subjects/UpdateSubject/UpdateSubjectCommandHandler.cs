using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.UpdateSubject;

public sealed class UpdateSubjectCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<UpdateSubjectCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateSubjectCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var subject = await dbContext.Subjects
            .FirstOrDefaultAsync(s => s.Id == command.SubjectId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Subject {command.SubjectId} not found.");

        if (command.ParentId is { } parentId)
        {
            bool parentExists = await dbContext.Subjects
                .AnyAsync(s => s.Id == parentId, cancellationToken)
                .ConfigureAwait(false);
            if (!parentExists)
            {
                throw new NotFoundException($"Subject {parentId} not found.");
            }
        }

        subject.Update(command.Name, command.ParentId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
