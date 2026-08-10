namespace FSH.Modules.People.Contracts.Dtos;

public sealed record TeacherDto(
    Guid Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    string DisplayName,
    string Phone,
    string Email,
    string? UserId,
    TeacherStatus Status,
    string? Bio,
    string[] Specializations,
    decimal? HourlyRate,
    Guid? AvatarFileId);
