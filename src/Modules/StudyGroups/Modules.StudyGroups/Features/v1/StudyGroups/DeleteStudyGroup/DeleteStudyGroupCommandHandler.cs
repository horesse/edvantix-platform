using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.DeleteStudyGroup;

public sealed class DeleteStudyGroupCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<DeleteStudyGroupCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteStudyGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        dbContext.StudyGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
