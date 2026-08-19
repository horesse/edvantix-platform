using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

/// <summary>Forming or Active → Cancelled. For groups that never ran or were aborted —
/// distinct from <see cref="FinishStudyGroupCommand"/>, which marks a normal completion.</summary>
public sealed record CancelStudyGroupCommand(Guid StudyGroupId, string? Reason = null) : ICommand<Unit>;
