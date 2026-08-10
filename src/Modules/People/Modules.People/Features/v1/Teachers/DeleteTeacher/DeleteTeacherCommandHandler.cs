using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Teachers.DeleteTeacher;

public sealed class DeleteTeacherCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<DeleteTeacherCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teacher = await dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == command.TeacherId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {command.TeacherId} not found.");

        dbContext.Teachers.Remove(teacher);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
