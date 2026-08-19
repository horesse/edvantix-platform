using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.Teachers;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers.RemoveGroupTeacher;

public sealed class RemoveGroupTeacherCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<RemoveGroupTeacherCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveGroupTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        group.RemoveTeacher(command.TeacherId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
