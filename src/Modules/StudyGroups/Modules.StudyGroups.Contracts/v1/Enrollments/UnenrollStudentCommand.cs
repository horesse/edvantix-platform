using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

/// <summary>Marks the enrollment <see cref="Dtos.EnrollmentStatus.Left"/> with a reason and
/// <c>LeftOn</c> — never deletes the row (see docs/02 Модули/StudyGroups.md → Инварианты,
/// "Зачисление — историческая запись").</summary>
public sealed record UnenrollStudentCommand(
    Guid StudyGroupId,
    Guid EnrollmentId,
    DateOnly? LeftOn = null,
    string? Reason = null) : ICommand<Unit>;
