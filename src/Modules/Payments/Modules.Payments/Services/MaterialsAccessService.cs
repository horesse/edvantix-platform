using FSH.Modules.Payments.Contracts;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Features.v1.StudentInvoices;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.People.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Services;

/// <summary>
/// EDX-015 — resolves whether a given user must lose access to lesson materials because a student
/// they are (or are responsible for) is overdue on payment. The only place the rule lives; both
/// Curriculum's enforcement and the cabinet's banner call through here.
/// </summary>
public sealed class MaterialsAccessService(
    PaymentsDbContext dbContext,
    ITenantSettingsService tenantSettings,
    IPeopleScopeResolver scopeResolver,
    TimeProvider timeProvider)
    : IMaterialsAccessService
{
    public async ValueTask<MaterialsAccessStatus> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await tenantSettings.GetCurrentAsync(cancellationToken).ConfigureAwait(false);
        var graceDays = settings.DebtGraceDays;

        // Flag off → nothing to check. This is the common path and costs one cached settings read.
        if (!settings.RestrictMaterialsOnDebt)
        {
            return new MaterialsAccessStatus(false, null, graceDays);
        }

        var scope = await scopeResolver.ResolveAsync(userId.ToString(), cancellationToken).ConfigureAwait(false);

        // Staff are never blocked: a teacher (even one whose own child studies here) manages
        // content; a manager/admin has no People rows at all. Only students and their guardians
        // are in scope for the block.
        if (scope.TeacherId is not null)
        {
            return new MaterialsAccessStatus(false, null, graceDays);
        }

        var studentIds = new List<Guid>(scope.WardStudentIds);
        if (scope.StudentId is { } studentId)
        {
            studentIds.Add(studentId);
        }
        if (studentIds.Count == 0)
        {
            return new MaterialsAccessStatus(false, null, graceDays);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var cutoff = today.AddDays(-graceDays);

        var overdueSince = await dbContext.StudentInvoices.AsNoTracking()
            .Where(i => studentIds.Contains(i.StudentId))
            .Where(StudentInvoiceQueries.OverdueBefore(cutoff))
            .OrderBy(i => i.DueDate)
            .Select(i => (DateOnly?)i.DueDate)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return overdueSince is null
            ? new MaterialsAccessStatus(false, null, graceDays)
            : new MaterialsAccessStatus(true, overdueSince, graceDays);
    }
}
