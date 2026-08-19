using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

public sealed record DeleteStudyGroupCommand(Guid StudyGroupId) : ICommand<Unit>;
