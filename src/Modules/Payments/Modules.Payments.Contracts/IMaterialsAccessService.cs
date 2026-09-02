namespace FSH.Modules.Payments.Contracts;

/// <summary>
/// The outcome of the EDX-015 "block lesson materials while a student is in arrears" rule for one
/// Identity user. <see cref="Restricted"/> already folds in the tenant flag
/// (<c>TenantSettings.RestrictMaterialsOnDebt</c>), the grace window, and who is exempt (staff /
/// teachers / unlinked users) — callers only render or enforce it.
/// </summary>
/// <param name="Restricted">True when this user's access to lesson materials must be blocked.</param>
/// <param name="OverdueSince">Earliest due date among the invoices that triggered the block, or
/// <c>null</c> when not restricted.</param>
/// <param name="GraceDays">The tenant's configured grace period, surfaced for the UI copy.</param>
public sealed record MaterialsAccessStatus(bool Restricted, DateOnly? OverdueSince, int GraceDays);

/// <summary>
/// Single source of truth for the EDX-015 materials-on-debt rule. Implemented in the Payments
/// runtime (it owns invoices and reads the tenant flag); consumed by Curriculum's
/// <c>LessonMaterialAccessPolicy</c> / lesson-materials query, and by the dashboard cabinet via
/// <c>GET /api/v1/student-invoices/my/materials-access</c>.
/// </summary>
public interface IMaterialsAccessService
{
    ValueTask<MaterialsAccessStatus> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
