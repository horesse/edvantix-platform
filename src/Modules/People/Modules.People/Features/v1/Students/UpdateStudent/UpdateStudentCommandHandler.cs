using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.UpdateStudent;

public sealed class UpdateStudentCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<UpdateStudentCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        student.Update(
            command.LastName,
            command.FirstName,
            command.MiddleName,
            command.BirthDate,
            command.Phone,
            command.Email,
            command.ManagerUserId,
            command.Source);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return student.Id;
    }
}
