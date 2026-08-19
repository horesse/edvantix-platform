namespace FSH.Modules.StudyGroups.Contracts.Dtos;

public sealed record StudyGroupDto(
    Guid Id,
    string Code,
    string Name,
    Guid CourseId,
    Guid PrimaryTeacherId,
    GroupFormat Format,
    int Capacity,
    int ActiveEnrollmentCount,
    DateOnly StartDate,
    DateOnly? EndDate,
    StudyGroupStatus Status,
    Guid? ChatChannelId,
    string? MeetingUrl,
    Guid? RoomId,
    string? Notes,
    DateTimeOffset CreatedAtUtc);
