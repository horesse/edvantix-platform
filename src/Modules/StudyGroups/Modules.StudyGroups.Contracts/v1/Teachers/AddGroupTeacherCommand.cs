using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Teachers;

public sealed record AddGroupTeacherCommand(
    Guid StudyGroupId,
    Guid TeacherId,
    TeacherRole Role) : ICommand<Guid>;
