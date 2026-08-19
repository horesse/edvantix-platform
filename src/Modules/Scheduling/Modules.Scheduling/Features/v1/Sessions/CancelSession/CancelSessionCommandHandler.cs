using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CancelSession;

public sealed class CancelSessionCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<CancelSessionCommand, Unit>
{
    public async ValueTask<Unit> Handle(CancelSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Session {command.SessionId} not found.");

        session.Cancel(command.Reason);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
