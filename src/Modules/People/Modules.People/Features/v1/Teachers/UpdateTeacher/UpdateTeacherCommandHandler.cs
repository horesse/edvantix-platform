using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Teachers.UpdateTeacher;

public sealed class UpdateTeacherCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<UpdateTeacherCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teacher = await dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == command.TeacherId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {command.TeacherId} not found.");

        teacher.Update(
            command.LastName,
            command.FirstName,
            command.MiddleName,
            command.Phone,
            command.Email,
            command.Bio,
            command.Specializations,
            command.HourlyRate);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return teacher.Id;
    }
}
