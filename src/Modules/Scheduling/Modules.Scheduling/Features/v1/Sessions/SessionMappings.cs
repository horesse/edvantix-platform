using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Domain;

namespace FSH.Modules.Scheduling.Features.v1.Sessions;

internal static class SessionMappings
{
    public static SessionDto ToDto(this Session s) => new(
        s.Id,
        s.StudyGroupId,
        s.LessonId,
        s.TeacherId,
        s.RoomId,
        s.StartUtc,
        s.EndUtc,
        s.Status,
        s.Topic,
        s.MeetingUrl,
        s.ScheduleTemplateId,
        s.RescheduledFromId);

    public static AttendanceDto ToDto(this Attendance a) => new(
        a.Id,
        a.SessionId,
        a.StudentId,
        a.Status,
        a.Comment,
        a.MarkedByUserId,
        a.MarkedAtUtc);

    public static CalendarEntryDto ToCalendarEntry(this Session s) => new(
        s.Id,
        s.StudyGroupId,
        s.TeacherId,
        s.RoomId,
        s.StartUtc,
        s.EndUtc,
        s.Status,
        s.Topic);
}
