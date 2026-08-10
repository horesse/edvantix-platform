namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>Single-student read shape (GetStudentByIdQuery) — kept separate from
/// <see cref="StudentDto"/> (search results) so the detail view can grow independently
/// (e.g. counts) without changing the paginated list contract.</summary>
public sealed record StudentDetailDto(
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
    DateTimeOffset EnrolledAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    int GuardianCount,
    int NoteCount);
