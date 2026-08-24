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

namespace FSH.Modules.People.Features.v1.Students.ArchiveStudent;

public sealed class ArchiveStudentCommandHandler(
    PeopleDbContext dbContext,
    [FromKeyedServices(typeof(PeopleDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<ArchiveStudentCommand, Unit>
{
    public async ValueTask<Unit> Handle(ArchiveStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        var from = student.Status;
        student.Archive();

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var now = TimeProvider.System.GetUtcNow();

        await outboxStore.AddAsync(
            new StudentStatusChangedIntegrationEvent(
                Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "People",
                student.Id, from, student.Status),
            cancellationToken).ConfigureAwait(false);

        await outboxStore.AddAsync(
            new StudentArchivedIntegrationEvent(
                Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "People",
                student.Id, now),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
