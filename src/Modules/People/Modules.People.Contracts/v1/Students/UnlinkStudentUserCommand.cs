using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record UnlinkStudentUserCommand(Guid StudentId) : ICommand<Unit>;
