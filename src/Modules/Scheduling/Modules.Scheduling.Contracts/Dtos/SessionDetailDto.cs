namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary><c>ResolvedTopic</c> is <c>Session.Topic</c>'s override, or the linked program
/// lesson's title when empty (ADR-006 "Тема занятия"). Materials are NOT included here — the
/// dashboard fetches those directly from Curriculum's own endpoints, per ADR-006's "Материалы
/// урока показываются на карточке занятия через запрос к Curriculum".</summary>
public sealed record SessionDetailDto(
    Guid Id,
    Guid StudyGroupId,
    Guid? LessonId,
    Guid TeacherId,
    Guid? RoomId,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    SessionStatus Status,
    string ResolvedTopic,
    string? MeetingUrl,
    string? CancelReason,
    Guid? RescheduledFromId,
    Guid? ScheduleTemplateId,
    string? TeacherComment,
    IReadOnlyList<AttendanceDto> Attendance);
