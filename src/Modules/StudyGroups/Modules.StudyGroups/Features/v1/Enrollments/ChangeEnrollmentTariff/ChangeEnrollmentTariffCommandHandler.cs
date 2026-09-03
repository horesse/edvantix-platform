using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.ChangeEnrollmentTariff;

/// <summary>Sets the enrollment's <c>TariffId</c>/<c>DiscountPercent</c> in place. No integration
/// event is published: Payments has no per-enrollment state to update — <c>BulkGenerateInvoicesCommand</c>
/// reads the tariff live from <c>IStudyGroupQueryService.GetActiveEnrollmentsWithTariffAsync</c> on
/// every run, so the next generation for the group picks up the new terms automatically
/// (mirrors the deliberately no-op <c>StudentEnrolledIntegrationEventHandler</c> in Payments).</summary>
public sealed class ChangeEnrollmentTariffCommandHandler(StudyGroupsDbContext dbContext)
    : ICommandHandler<ChangeEnrollmentTariffCommand, Unit>
{
    public async ValueTask<Unit> Handle(ChangeEnrollmentTariffCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var group = await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .FirstOrDefaultAsync(g => g.Enrollments.Any(e => e.Id == command.EnrollmentId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Enrollment {command.EnrollmentId} not found.");

        group.ChangeEnrollmentTariff(command.EnrollmentId, command.TariffId, command.DiscountPercent);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
