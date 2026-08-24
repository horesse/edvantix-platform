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

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.UnenrollStudent;

public sealed class UnenrollStudentCommandHandler(
    StudyGroupsDbContext dbContext,
    [FromKeyedServices(typeof(StudyGroupsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<UnenrollStudentCommand, Unit>
{
    public async ValueTask<Unit> Handle(UnenrollStudentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Id == command.StudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.StudyGroupId} not found.");

        var enrollment = group.Enrollments.FirstOrDefault(e => e.Id == command.EnrollmentId)
            ?? throw new NotFoundException($"Enrollment {command.EnrollmentId} not found in study group {group.Id}.");
        var studentId = enrollment.StudentId;

        var leftOn = command.LeftOn ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        group.Unenroll(command.EnrollmentId, leftOn, command.Reason);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        await outboxStore.AddAsync(
            new StudentUnenrolledIntegrationEvent(
                Guid.NewGuid(), timeProvider.GetUtcNow().UtcDateTime, tenantId, Guid.NewGuid().ToString(), "StudyGroups",
                group.Id, studentId, leftOn, command.Reason),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
