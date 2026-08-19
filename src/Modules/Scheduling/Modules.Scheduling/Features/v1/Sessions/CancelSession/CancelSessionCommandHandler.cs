using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.Events;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CancelSession;

public sealed class CancelSessionCommandHandler(
    SchedulingDbContext dbContext,
    [FromKeyedServices(typeof(SchedulingDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<CancelSessionCommand, Unit>
{
    public async ValueTask<Unit> Handle(CancelSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = await dbContext.Sessions
            .FirstOrDefaultAsync(s => s.Id == command.SessionId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Session {command.SessionId} not found.");

        bool wasAlreadyCancelled = session.Status == SessionStatus.Cancelled;
        session.Cancel(command.Reason);

        if (!wasAlreadyCancelled)
        {
            var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
            await outboxStore.AddAsync(
                new SessionCancelledIntegrationEvent(
                    Id: Guid.NewGuid(),
                    OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
                    TenantId: tenantId,
                    CorrelationId: Guid.NewGuid().ToString(),
                    Source: "Scheduling",
                    SessionId: session.Id,
                    StudyGroupId: session.StudyGroupId,
                    Reason: session.CancelReason),
                cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
