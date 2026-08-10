using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Teachers.DeactivateTeacher;

public sealed class DeactivateTeacherCommandHandler(
    PeopleDbContext dbContext,
    IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<DeactivateTeacherCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeactivateTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var teacher = await dbContext.Teachers
            .FirstOrDefaultAsync(t => t.Id == command.TeacherId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Teacher {command.TeacherId} not found.");

        teacher.Deactivate();

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new TeacherDeactivatedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "People", teacher.Id),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
