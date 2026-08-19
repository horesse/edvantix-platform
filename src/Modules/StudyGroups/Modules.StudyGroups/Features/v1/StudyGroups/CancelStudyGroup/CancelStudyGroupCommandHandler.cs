using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.CancelStudyGroup;

public sealed class CancelStudyGroupCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<CancelStudyGroupCommand, Unit>
{
    public async ValueTask<Unit> Handle(CancelStudyGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        group.Cancel(command.Reason);

        // No integration event for Cancel — not documented in docs/02 Модули/StudyGroups.md →
        // "Публикуемые события" (only Created/Activated/Finished are). A cancelled group never
        // had students actively attending (Cancel is only reachable from Forming/Active with no
        // completion semantics), so nothing downstream needs to react.
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
