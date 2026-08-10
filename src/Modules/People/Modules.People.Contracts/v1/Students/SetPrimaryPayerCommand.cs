using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record SetPrimaryPayerCommand(Guid StudentId, Guid GuardianId) : ICommand<Unit>;
