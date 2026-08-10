using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record RemoveStudentGuardianCommand(Guid StudentId, Guid GuardianId) : ICommand<Unit>;
