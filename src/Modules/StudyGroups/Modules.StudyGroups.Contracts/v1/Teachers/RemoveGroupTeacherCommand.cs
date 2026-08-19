using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Teachers;

public sealed record RemoveGroupTeacherCommand(Guid StudyGroupId, Guid TeacherId) : ICommand<Unit>;
