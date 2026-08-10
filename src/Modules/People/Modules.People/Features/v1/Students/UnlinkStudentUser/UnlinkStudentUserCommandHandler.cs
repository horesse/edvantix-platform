using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Students.UnlinkStudentUser;

public sealed class UnlinkStudentUserCommandHandler(PeopleDbContext dbContext, HybridCache cache)
    : ICommandHandler<UnlinkStudentUserCommand, Unit>
{
    public async ValueTask<Unit> Handle(UnlinkStudentUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        var previousUserId = student.UserId;
        student.UnlinkUser();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (previousUserId is not null)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.User(previousUserId), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
