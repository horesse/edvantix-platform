using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Guardians.DeleteGuardian;

public sealed class DeleteGuardianCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<DeleteGuardianCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteGuardianCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var guardian = await dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == command.GuardianId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Guardian {command.GuardianId} not found.");

        dbContext.Guardians.Remove(guardian);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
