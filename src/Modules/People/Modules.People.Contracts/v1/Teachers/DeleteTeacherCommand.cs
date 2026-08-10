using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record DeleteTeacherCommand(Guid TeacherId) : ICommand<Unit>;
