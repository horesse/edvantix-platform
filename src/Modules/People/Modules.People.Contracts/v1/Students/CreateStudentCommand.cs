using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record CreateStudentCommand(
    string LastName,
    string FirstName,
    string? MiddleName,
    DateOnly BirthDate,
    string Phone,
    string Email,
    string ManagerUserId,
    string? Source = null) : ICommand<Guid>;
