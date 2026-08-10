using System.Net;
using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Teachers.LinkTeacherUser;

public sealed class LinkTeacherUserCommandHandler(PeopleDbContext dbContext, IUserService userService, HybridCache cache)
    : ICommandHandler<LinkTeacherUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(LinkTeacherUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teacher = await dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == command.TeacherId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {command.TeacherId} not found.");

        await userService.GetAsync(command.UserId, cancellationToken).ConfigureAwait(false);

        bool alreadyLinked = await dbContext.Teachers
            .AnyAsync(t => t.UserId == command.UserId && t.Id != command.TeacherId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyLinked)
        {
            throw new CustomException(
                $"User {command.UserId} is already linked to another teacher.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        teacher.LinkUser(command.UserId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.User(command.UserId), cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
