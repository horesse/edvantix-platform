namespace FSH.Modules.People.Contracts.Dtos;

public sealed record StudentDto(
    Guid Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    string DisplayName,
    DateOnly BirthDate,
    string Phone,
    string Email,
    string? UserId,
    StudentStatus Status,
    string? Source,
    Guid? AvatarFileId,
    string ManagerUserId,
    DateTimeOffset EnrolledAtUtc);
