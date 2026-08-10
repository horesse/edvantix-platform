using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record DeleteStudentCommand(Guid StudentId) : ICommand<Unit>;
