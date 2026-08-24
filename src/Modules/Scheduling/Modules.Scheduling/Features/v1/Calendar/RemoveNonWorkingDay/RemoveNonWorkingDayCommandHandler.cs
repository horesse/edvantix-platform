using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.RemoveNonWorkingDay;

public sealed class RemoveNonWorkingDayCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<RemoveNonWorkingDayCommand, Unit>
{
    public async ValueTask<Unit> Handle(RemoveNonWorkingDayCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var nonWorkingDay = await dbContext.NonWorkingDays
            .FirstOrDefaultAsync(d => d.Id == command.NonWorkingDayId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"NonWorkingDay {command.NonWorkingDayId} not found.");

        dbContext.NonWorkingDays.Remove(nonWorkingDay);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
