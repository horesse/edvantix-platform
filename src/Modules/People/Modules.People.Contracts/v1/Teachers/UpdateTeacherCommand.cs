using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record UpdateTeacherCommand(
    Guid TeacherId,
    string LastName,
    string FirstName,
    string? MiddleName,
    string Phone,
    string Email,
    string? Bio = null,
    string[]? Specializations = null,
    decimal? HourlyRate = null) : ICommand<Guid>;
