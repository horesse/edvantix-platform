using Mediator;

namespace FSH.Modules.People.Contracts.v1.Guardians;

public sealed record UnlinkGuardianUserCommand(Guid GuardianId) : ICommand<Unit>;
