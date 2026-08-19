namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record SessionDto(
    Guid Id,
    Guid StudyGroupId,
    Guid? LessonId,
    Guid TeacherId,
    Guid? RoomId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    SessionStatus Status,
    string? Topic,
    string? MeetingUrl,
    Guid? ScheduleTemplateId,
    Guid? RescheduledFromId);
