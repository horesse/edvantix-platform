using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;

namespace FSH.Modules.People.Features.v1.Guardians.CreateGuardian;

public sealed class CreateGuardianCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<CreateGuardianCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateGuardianCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var guardian = Guardian.Create(command.LastName, command.FirstName, command.Phone, command.Email);
        dbContext.Guardians.Add(guardian);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return guardian.Id;
    }
}
