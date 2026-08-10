using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Guardians.UpdateGuardian;

public sealed class UpdateGuardianCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<UpdateGuardianCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateGuardianCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var guardian = await dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == command.GuardianId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Guardian {command.GuardianId} not found.");

        guardian.Update(command.LastName, command.FirstName, command.Phone, command.Email);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return guardian.Id;
    }
}
