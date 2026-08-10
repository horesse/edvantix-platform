using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.SetPrimaryPayer;

public sealed class SetPrimaryPayerCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<SetPrimaryPayerCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetPrimaryPayerCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .Include(s => s.GuardianLinks)
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        try
        {
            student.SetPrimaryPayer(command.GuardianId);
        }
        catch (InvalidOperationException ex)
        {
            throw new NotFoundException(ex.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
