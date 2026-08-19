using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.UpdateStudyGroup;

public sealed class UpdateStudyGroupCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<UpdateStudyGroupCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Enrollments included: StudyGroup.Update checks the new Capacity against
        // ActiveEnrollmentCount, which reads the in-memory collection.
        var group = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        group.Update(
            command.Name,
            command.PrimaryTeacherId,
            command.Format,
            command.Capacity,
            command.StartDate,
            command.EndDate,
            command.MeetingUrl,
            command.RoomId,
            command.Notes);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
