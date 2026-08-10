using FSH.Framework.Core.Domain;

namespace FSH.Modules.People.Domain;

/// <summary>
/// Links a <see cref="Student"/> to a <see cref="Guardian"/> (parent/relative/sponsor).
/// Owned by <see cref="Student"/> (mutated only through the aggregate) so the "exactly one
/// primary payer" invariant holds — same shape as <c>Product.Images</c>/<c>IsThumbnail</c>
/// in the Catalog module.
/// <para>
/// Soft-deletable rather than hard-deleted: removing a guardian keeps the historical link
/// (who used to be responsible for this student) instead of losing it outright.
/// </para>
/// </summary>
public sealed class StudentGuardian : BaseEntity<Guid>, ISoftDeletable
{
    public Guid StudentId { get; private set; }
    public Guid GuardianId { get; private set; }
    public string Relation { get; private set; } = default!;
    public bool IsPrimaryPayer { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private StudentGuardian() { }

    internal static StudentGuardian Create(Guid studentId, Guid guardianId, string relation, bool isPrimaryPayer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relation);
        if (guardianId == Guid.Empty)
        {
            throw new ArgumentException("GuardianId is required.", nameof(guardianId));
        }

        return new StudentGuardian
        {
            Id = Guid.CreateVersion7(),
            StudentId = studentId,
            GuardianId = guardianId,
            Relation = relation.Trim(),
            IsPrimaryPayer = isPrimaryPayer,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }

    internal void MarkPrimaryPayer(bool value) => IsPrimaryPayer = value;

    public void Restore()
    {
        if (!IsDeleted)
        {
            return;
        }

        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
    }
}
