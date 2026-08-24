using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.StudyGroups.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.EnrollStudents;

public sealed class EnrollStudentsCommandHandler(
    StudyGroupsDbContext dbContext,
    [FromKeyedServices(typeof(StudyGroupsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<EnrollStudentsCommand, IReadOnlyList<Guid>>
{
    public async ValueTask<IReadOnlyList<Guid>> Handle(EnrollStudentsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        var enrolledOn = command.EnrolledOn ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var now = timeProvider.GetUtcNow();

        // group.Enroll checks Capacity against the running count on every call — if student N would
        // exceed it, the exception propagates before SaveChangesAsync runs, so nothing in the batch
        // is persisted (see docs/02 Модули/StudyGroups.md → Контракты, EnrollStudentsCommand remarks).
        var newEnrollmentIds = new List<Guid>(command.StudentIds.Count);
        foreach (var studentId in command.StudentIds)
        {
            var enrollment = group.Enroll(studentId, enrolledOn, command.TariffId, command.DiscountPercent);
            newEnrollmentIds.Add(enrollment.Id);

            await outboxStore.AddAsync(
                new StudentEnrolledIntegrationEvent(
                    Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "StudyGroups",
                    group.Id, studentId, enrolledOn, command.TariffId),
                cancellationToken).ConfigureAwait(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return newEnrollmentIds;
    }
}
