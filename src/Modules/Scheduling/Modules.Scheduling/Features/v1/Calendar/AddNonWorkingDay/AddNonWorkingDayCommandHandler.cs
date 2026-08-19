using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.AddNonWorkingDay;

public sealed class AddNonWorkingDayCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<AddNonWorkingDayCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddNonWorkingDayCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool exists = await dbContext.NonWorkingDays
            .AnyAsync(d => d.Date == command.Date, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
        {
            throw new CustomException(
                $"'{command.Date}' is already a non-working day.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        var nonWorkingDay = NonWorkingDay.Create(command.Date, command.Description);

        dbContext.NonWorkingDays.Add(nonWorkingDay);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return nonWorkingDay.Id;
    }
}
