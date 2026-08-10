using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record DeactivateTeacherCommand(Guid TeacherId) : ICommand<Unit>;
