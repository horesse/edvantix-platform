using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

/// <summary>Atomically closes the source enrollment (<see cref="Dtos.EnrollmentStatus.Left"/>,
/// reason "Transfer") and creates a new one in <paramref name="TargetStudyGroupId"/> — both writes
/// happen in the same <c>SaveChangesAsync</c>, see docs/02 Модули/StudyGroups.md → Контракты.</summary>
public sealed record TransferEnrollmentCommand(
    Guid EnrollmentId,
    Guid TargetStudyGroupId,
    DateOnly? TransferDate = null) : ICommand<Guid>;
