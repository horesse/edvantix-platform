using Mediator;

namespace FSH.Modules.People.Contracts.v1.Guardians;

public sealed record LinkGuardianUserCommand(Guid GuardianId, string UserId) : ICommand<Unit>;
