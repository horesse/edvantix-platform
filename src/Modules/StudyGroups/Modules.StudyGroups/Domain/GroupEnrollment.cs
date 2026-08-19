using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.StudyGroups.Contracts.Dtos;

namespace FSH.Modules.StudyGroups.Domain;

/// <summary>
/// A student's enrollment in a <see cref="StudyGroup"/>. Owned by the group (mutated only through
/// <see cref="StudyGroup.Enroll"/>/<see cref="StudyGroup.Unenroll"/>/etc.), never physically deleted —
/// "Зачисление — историческая запись" (see docs/02 Модули/StudyGroups.md → Инварианты): leaving is
/// <see cref="EnrollmentStatus.Left"/> plus <see cref="LeftOn"/>/<see cref="LeaveReason"/>, not a row
/// removal, so attendance and invoices from before the departure stay intact. Re-enrollment after
/// <see cref="EnrollmentStatus.Left"/> creates a brand new row (see <see cref="StudyGroup.Enroll"/>).
/// <para>
/// Not <see cref="ISoftDeletable"/>: there is no command in the contracts for hard-deleting a
/// mistakenly created row, so that half of the invariant ("мягкое удаление — только для ошибочно
/// созданных записей") is intentionally deferred, not implemented.
/// </para>
/// </summary>
public sealed class GroupEnrollment : BaseEntity<Guid>
{
    public Guid StudyGroupId { get; private set; }
    public Guid StudentId { get; private set; }
    public DateOnly EnrolledOn { get; private set; }
    public DateOnly? LeftOn { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public string? LeaveReason { get; private set; }
    public Guid? TariffId { get; private set; }
    public decimal DiscountPercent { get; private set; }

    private GroupEnrollment() { }

    internal static GroupEnrollment Create(
        Guid studyGroupId, Guid studentId, DateOnly enrolledOn, Guid? tariffId, decimal discountPercent)
    {
        if (discountPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(discountPercent), "DiscountPercent must be between 0 and 100.");
        }

        return new GroupEnrollment
        {
            Id = Guid.CreateVersion7(),
            StudyGroupId = studyGroupId,
            StudentId = studentId,
            EnrolledOn = enrolledOn,
            Status = EnrollmentStatus.Active,
            TariffId = tariffId,
            DiscountPercent = discountPercent,
        };
    }

    internal void MarkLeft(DateOnly leftOn, string? reason)
    {
        if (Status == EnrollmentStatus.Left)
        {
            return;
        }
        if (Status == EnrollmentStatus.Completed)
        {
            throw new CustomException(
                "Cannot unenroll a completed enrollment.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = EnrollmentStatus.Left;
        LeftOn = leftOn;
        LeaveReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    internal void Pause()
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new CustomException(
                $"Cannot pause an enrollment in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = EnrollmentStatus.Paused;
    }

    internal void Resume()
    {
        if (Status != EnrollmentStatus.Paused)
        {
            throw new CustomException(
                $"Cannot resume an enrollment in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = EnrollmentStatus.Active;
    }

    /// <summary>Active/Paused → Completed — bulk-applied by <see cref="StudyGroup.Finish"/> so a
    /// finished group's roster reads as "who finished the course".</summary>
    internal void Complete()
    {
        if (Status is EnrollmentStatus.Left or EnrollmentStatus.Completed)
        {
            return;
        }

        Status = EnrollmentStatus.Completed;
    }
}
