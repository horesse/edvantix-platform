using FSH.Framework.Caching;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace FSH.Modules.People.Features.v1.Students.RemoveStudentGuardian;

public sealed class RemoveStudentGuardianCommandHandler(PeopleDbContext dbContext, HybridCache cache)
    : ICommandHandler<RemoveStudentGuardianCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveStudentGuardianCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .Include(s => s.GuardianLinks)
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        try
        {
            student.RemoveGuardianLink(command.GuardianId);
        }
        catch (InvalidOperationException ex)
        {
            throw new NotFoundException(ex.Message);
        }

        // Removing from the tracked collection marks the (required-FK) link as deleted — the
        // audit interceptor turns that into a soft-delete update on save (see database.md).
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var guardianUserId = await dbContext.Guardians
            .AsNoTracking()
            .Where(g => g.Id == command.GuardianId)
            .Select(g => g.UserId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (guardianUserId is not null)
        {
            await cache.RemoveByTagAsync(CacheKeys.Tags.User(guardianUserId), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
