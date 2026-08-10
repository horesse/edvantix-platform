using System.Net;
using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Guardians.LinkGuardianUser;

public sealed class LinkGuardianUserCommandHandler(PeopleDbContext dbContext, IUserService userService, HybridCache cache)
    : ICommandHandler<LinkGuardianUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(LinkGuardianUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var guardian = await dbContext.Guardians
            .FirstOrDefaultAsync(g => g.Id == command.GuardianId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Guardian {command.GuardianId} not found.");

        await userService.GetAsync(command.UserId, cancellationToken).ConfigureAwait(false);

        bool alreadyLinked = await dbContext.Guardians
            .AnyAsync(g => g.UserId == command.UserId && g.Id != command.GuardianId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyLinked)
        {
            throw new CustomException(
                $"User {command.UserId} is already linked to another guardian.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        guardian.LinkUser(command.UserId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.User(command.UserId), cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
