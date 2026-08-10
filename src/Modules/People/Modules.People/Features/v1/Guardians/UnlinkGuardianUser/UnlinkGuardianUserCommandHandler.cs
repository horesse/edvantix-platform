using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Guardians.UnlinkGuardianUser;

public sealed class UnlinkGuardianUserCommandHandler(PeopleDbContext dbContext, HybridCache cache)
    : ICommandHandler<UnlinkGuardianUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(UnlinkGuardianUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var guardian = await dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == command.GuardianId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Guardian {command.GuardianId} not found.");

        var previousUserId = guardian.UserId;
        guardian.UnlinkUser();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (previousUserId is not null)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.User(previousUserId), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
