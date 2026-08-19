using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.People.Contracts.Events;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;

namespace FSH.Modules.People.Features.v1.Students.CreateStudent;

public sealed class CreateStudentCommandHandler(
    PeopleDbContext dbContext,
    [FromKeyedServices(typeof(PeopleDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor)
    : ICommandHandler<CreateStudentCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

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

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
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
