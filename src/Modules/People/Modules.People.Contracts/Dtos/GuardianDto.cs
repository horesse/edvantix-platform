namespace FSH.Modules.People.Contracts.Dtos;

public sealed record GuardianDto(
    Guid Id,
    string LastName,
    string FirstName,
    string DisplayName,
    string Phone,
    string Email,
    string? UserId);
