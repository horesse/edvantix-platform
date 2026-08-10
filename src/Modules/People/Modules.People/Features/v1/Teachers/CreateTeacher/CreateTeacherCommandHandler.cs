using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;

namespace FSH.Modules.People.Features.v1.Teachers.CreateTeacher;

public sealed class CreateTeacherCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<CreateTeacherCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teacher = Teacher.Create(
            command.LastName,
            command.FirstName,
            command.MiddleName,
            command.Phone,
            command.Email,
            command.Bio,
            command.Specializations,
            command.HourlyRate);

        dbContext.Teachers.Add(teacher);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return teacher.Id;
    }
}
