using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;

namespace FSH.Modules.People.Features.v1.Students.CreateStudent;

public sealed class CreateStudentCommandHandler(
    PeopleDbContext dbContext,
    [FromKeyedServices(typeof(PeopleDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IQuotaService quotas)
    : ICommandHandler<CreateStudentCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            // Soft plan-limit block: a tenant at its ActiveStudents ceiling gets 402, existing
            // students stay fully accessible. Restore/reactivate of an archived student is not gated.
            await quotas.EnsureHeadroomAsync(tenantId, QuotaResource.ActiveStudents, 1, cancellationToken)
                .ConfigureAwait(false);
        }

        var student = Student.Create(
            command.LastName,
            command.FirstName,
            command.MiddleName,
            command.BirthDate,
            command.Phone,
            command.Email,
            command.ManagerUserId,
            command.Source);

        dbContext.Students.Add(student);

        await outboxStore.AddAsync(
            new StudentCreatedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: TimeProvider.System.GetUtcNow().UtcDateTime,
                TenantId: tenantId,
                CorrelationId: Guid.NewGuid().ToString(),
                Source: "People",
                StudentId: student.Id,
                LastName: student.LastName,
                FirstName: student.FirstName),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return student.Id;
    }
}
