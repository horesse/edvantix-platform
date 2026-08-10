using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record LinkStudentUserCommand(Guid StudentId, string UserId) : ICommand<Unit>;
