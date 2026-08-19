using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.RestoreStudent;

public sealed class RestoreStudentCommandHandler(
    PeopleDbContext dbContext,
    [FromKeyedServices(typeof(PeopleDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<RestoreStudentCommand, Unit>
{
    public async ValueTask<Unit> Handle(RestoreStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        var from = student.Status;
        student.Reactivate();

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new StudentStatusChangedIntegrationEvent(
                Guid.NewGuid(), TimeProvider.System.GetUtcNow().UtcDateTime, tenantId,
                Guid.NewGuid().ToString(), "People", student.Id, from, student.Status),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
