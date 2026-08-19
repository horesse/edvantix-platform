using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.PauseEnrollment;

public sealed class PauseEnrollmentCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<PauseEnrollmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(PauseEnrollmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Enrollments.Any(e => e.Id == command.EnrollmentId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Enrollment {command.EnrollmentId} not found.");

        group.PauseEnrollment(command.EnrollmentId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
