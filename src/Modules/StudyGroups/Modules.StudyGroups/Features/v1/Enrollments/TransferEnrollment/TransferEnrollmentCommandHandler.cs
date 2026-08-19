using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.StudyGroups.Contracts.Events;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.TransferEnrollment;

/// <summary>Closes the enrollment in its current group and opens a new one in the target group —
/// both writes land in the same <c>SaveChangesAsync</c> call (EF Core wraps it in one transaction),
/// so a transfer is never left half-applied.</summary>
public sealed class TransferEnrollmentCommandHandler(
    StudyGroupsDbContext dbContext,
    [FromKeyedServices(typeof(StudyGroupsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<TransferEnrollmentCommand, Guid>
{
    public async ValueTask<Guid> Handle(TransferEnrollmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sourceGroup = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Enrollments.Any(e => e.Id == command.EnrollmentId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Enrollment {command.EnrollmentId} not found.");

        var sourceEnrollment = sourceGroup.Enrollments.First(e => e.Id == command.EnrollmentId);

        if (command.TargetStudyGroupId == sourceGroup.Id)
        {
            throw new CustomException(
                "Target study group must be different from the current one.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        var targetGroup = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Id == command.TargetStudyGroupId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Study group {command.TargetStudyGroupId} not found.");

        var transferDate = command.TransferDate ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var studentId = sourceEnrollment.StudentId;

        // Preserve the student's commercial terms across the move — a transfer is an
        // administrative reassignment, not a new enrollment contract.
        var tariffId = sourceEnrollment.TariffId;
        var discountPercent = sourceEnrollment.DiscountPercent;

        sourceGroup.Unenroll(command.EnrollmentId, transferDate, $"Transfer to {targetGroup.Code}");
        var newEnrollment = targetGroup.Enroll(studentId, transferDate, tariffId, discountPercent);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var now = timeProvider.GetUtcNow();

        await outboxStore.AddAsync(
            new StudentUnenrolledIntegrationEvent(
                Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "StudyGroups",
                sourceGroup.Id, studentId, transferDate, $"Transfer to {targetGroup.Code}"),
            cancellationToken).ConfigureAwait(false);

        await outboxStore.AddAsync(
            new StudentEnrolledIntegrationEvent(
                Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "StudyGroups",
                targetGroup.Id, studentId, transferDate, tariffId),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return newEnrollment.Id;
    }
}
