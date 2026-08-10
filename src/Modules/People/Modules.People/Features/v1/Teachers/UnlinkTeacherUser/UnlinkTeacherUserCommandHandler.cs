using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Teachers.UnlinkTeacherUser;

public sealed class UnlinkTeacherUserCommandHandler(PeopleDbContext dbContext, HybridCache cache)
    : ICommandHandler<UnlinkTeacherUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(UnlinkTeacherUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teacher = await dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == command.TeacherId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {command.TeacherId} not found.");

        var previousUserId = teacher.UserId;
        teacher.UnlinkUser();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (previousUserId is not null)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.User(previousUserId), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
