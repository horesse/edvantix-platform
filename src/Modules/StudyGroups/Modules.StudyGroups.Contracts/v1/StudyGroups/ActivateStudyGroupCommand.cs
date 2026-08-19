using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

/// <summary><see cref="Dtos.StudyGroupStatus.Forming"/> → <see cref="Dtos.StudyGroupStatus.Active"/>.
/// Requires at least one enrollment (see docs/02 Модули/StudyGroups.md → Инварианты).</summary>
public sealed record ActivateStudyGroupCommand(Guid StudyGroupId) : ICommand<Unit>;
