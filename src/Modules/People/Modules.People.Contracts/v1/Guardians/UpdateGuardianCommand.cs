using Mediator;

namespace FSH.Modules.People.Contracts.v1.Guardians;

public sealed record UpdateGuardianCommand(
    Guid GuardianId,
    string LastName,
    string FirstName,
    string Phone,
    string Email) : ICommand<Guid>;
