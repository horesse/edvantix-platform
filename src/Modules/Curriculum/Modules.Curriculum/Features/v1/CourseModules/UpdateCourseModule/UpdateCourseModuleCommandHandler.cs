using FSH.Framework.Core.Exceptions;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using FSH.Modules.Curriculum.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.UpdateCourseModule;

public sealed class UpdateCourseModuleCommandHandler(CurriculumDbContext dbContext)
    : ICommandHandler<UpdateCourseModuleCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateCourseModuleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var module = await dbContext.CourseModules
            .FirstOrDefaultAsync(m => m.Id == command.CourseModuleId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Course module {command.CourseModuleId} not found.");

        module.Update(command.Title, command.Description);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
