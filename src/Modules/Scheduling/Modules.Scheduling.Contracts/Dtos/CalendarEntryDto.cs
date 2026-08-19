namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>One block on the calendar view — same shape as <c>SessionDto</c> minus the
/// bookkeeping-only fields (<c>ScheduleTemplateId</c>/<c>RescheduledFromId</c>) it doesn't need.</summary>
public sealed record CalendarEntryDto(
    Guid SessionId,
    Guid StudyGroupId,
    Guid TeacherId,
    Guid? RoomId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    SessionStatus Status,
    string? Topic);
