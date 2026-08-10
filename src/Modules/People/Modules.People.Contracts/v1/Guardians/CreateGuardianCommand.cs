using Mediator;

namespace FSH.Modules.People.Contracts.v1.Guardians;

public sealed record CreateGuardianCommand(
    string LastName,
    string FirstName,
    string Phone,
    string Email) : ICommand<Guid>;
