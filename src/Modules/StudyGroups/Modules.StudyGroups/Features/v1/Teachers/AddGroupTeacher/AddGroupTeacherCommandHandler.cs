using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.Teachers;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers.AddGroupTeacher;

public sealed class AddGroupTeacherCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<AddGroupTeacherCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddGroupTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .Include(g => g.Teachers)
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        var teacher = group.AddTeacher(command.TeacherId, command.Role);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return teacher.Id;
    }
}
