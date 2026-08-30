using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Quota;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Quota;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;

namespace FSH.Modules.People.Features.v1.Teachers.CreateTeacher;

public sealed class CreateTeacherCommandHandler(
    PeopleDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    IQuotaService quotas)
    : ICommandHandler<CreateTeacherCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTeacherCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            // Soft plan-limit block (402) on the ActiveTeachers ceiling. Reactivating an inactive
            // teacher is not gated — it's not a new entity.
            await quotas.EnsureHeadroomAsync(tenantId, QuotaResource.ActiveTeachers, 1, cancellationToken)
                .ConfigureAwait(false);
        }

        var teacher = Teacher.Create(
            command.LastName,
            command.FirstName,
            command.MiddleName,
            command.Phone,
            command.Email,
            command.Bio,
            command.Specializations,
            command.HourlyRate);

        dbContext.Teachers.Add(teacher);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return teacher.Id;
    }
}
