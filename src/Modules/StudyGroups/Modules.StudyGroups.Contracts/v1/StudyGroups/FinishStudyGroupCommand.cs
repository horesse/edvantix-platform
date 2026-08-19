using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

/// <summary><see cref="Dtos.StudyGroupStatus.Active"/> → <see cref="Dtos.StudyGroupStatus.Finished"/>.
/// Freezes the roster — no further enrollment/unenrollment/transfer commands are accepted
/// afterward (see docs/02 Модули/StudyGroups.md → Инварианты).</summary>
public sealed record FinishStudyGroupCommand(Guid StudyGroupId, DateOnly? FinishedOn = null) : ICommand<Unit>;
