namespace FSH.Modules.StudyGroups.Contracts.Dtos;

/// <summary>
/// <see cref="StudyGroupDto"/> plus the roster — composed in the handler by joining
/// <c>GroupEnrollments</c>/<c>GroupTeachers</c> explicitly (same reasoning as Curriculum's
/// <c>CourseDetailDto</c>: an independent <c>DbSet</c> per entity, not an owned-collection nav load).
/// </summary>
public sealed record StudyGroupDetailDto(
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
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<GroupEnrollmentDto> Enrollments,
    IReadOnlyList<GroupTeacherDto> Teachers);
