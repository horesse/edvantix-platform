using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.DeleteScheduleTemplate;

public sealed class DeleteScheduleTemplateCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<DeleteScheduleTemplateCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteScheduleTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await dbContext.ScheduleTemplates
            .FirstOrDefaultAsync(t => t.Id == command.ScheduleTemplateId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ScheduleTemplate {command.ScheduleTemplateId} not found.");

        // Already-generated Session rows keep their ScheduleTemplateId as-is (no DB-level FK) —
        // deleting a template does not touch history, only stops future generation.
        dbContext.ScheduleTemplates.Remove(template);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
