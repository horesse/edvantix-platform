using System.Net;
using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Students.LinkStudentUser;

public sealed class LinkStudentUserCommandHandler(PeopleDbContext dbContext, IUserService userService, HybridCache cache)
    : ICommandHandler<LinkStudentUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(LinkStudentUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        // Throws NotFoundException itself if the account doesn't exist in Identity.
        await userService.GetAsync(command.UserId, cancellationToken).ConfigureAwait(false);

        bool alreadyLinked = await dbContext.Students
            .AnyAsync(s => s.UserId == command.UserId && s.Id != command.StudentId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyLinked)
        {
            throw new CustomException(
                $"User {command.UserId} is already linked to another student.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        student.LinkUser(command.UserId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await cache.RemoveByTagAsync(CacheKeys.Tags.User(command.UserId), cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
