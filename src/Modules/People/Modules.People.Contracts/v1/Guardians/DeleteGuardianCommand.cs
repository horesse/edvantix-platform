using Mediator;

namespace FSH.Modules.People.Contracts.v1.Guardians;

public sealed record DeleteGuardianCommand(Guid GuardianId) : ICommand<Unit>;
