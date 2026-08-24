using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.UpdateScheduleTemplate;

public sealed class UpdateScheduleTemplateCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<UpdateScheduleTemplateCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateScheduleTemplateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var template = await dbContext.ScheduleTemplates
            .FirstOrDefaultAsync(t => t.Id == command.ScheduleTemplateId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ScheduleTemplate {command.ScheduleTemplateId} not found.");

        template.Update(
            command.DayOfWeek,
            command.StartTime,
            command.DurationMinutes,
            command.RoomId,
            command.TeacherId,
            command.ValidFrom,
            command.ValidTo,
            command.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
