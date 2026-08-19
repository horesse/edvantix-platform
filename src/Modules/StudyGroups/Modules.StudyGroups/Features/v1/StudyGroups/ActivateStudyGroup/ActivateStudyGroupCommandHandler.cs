using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.StudyGroups.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.ActivateStudyGroup;

public sealed class ActivateStudyGroupCommandHandler(
    StudyGroupsDbContext dbContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<ActivateStudyGroupCommand, Unit>
{
    public async ValueTask<Unit> Handle(ActivateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Enrollments included: StudyGroup.Activate requires ActiveEnrollmentCount > 0.
        var group = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        group.Activate();

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new StudyGroupActivatedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "StudyGroups", group.Id),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
