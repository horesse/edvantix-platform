using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.Dtos;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A calendar event for a study group — "занятие", as opposed to Curriculum's <c>Lesson</c>
/// ("урок программы"), see ADR-006. <see cref="LessonId"/> is nullable: trial lessons, make-up
/// sessions and consultations don't correspond to a program lesson. Flat persistence (own
/// <c>DbSet</c>, not owned by <see cref="ScheduleTemplate"/>) — sessions are searched/paged/reported
/// on independently of any template, same reasoning as Curriculum's <c>Lesson</c> being flat rather
/// than nested under <c>Course</c>.
/// </summary>
public sealed class Session : BaseEntity<Guid>
{
    public Guid StudyGroupId { get; private set; }
    public Guid? LessonId { get; private set; }
    public Guid TeacherId { get; private set; }
    public Guid? RoomId { get; private set; }
    public DateTimeOffset StartUtc { get; private set; }
    public DateTimeOffset EndUtc { get; private set; }
    public SessionStatus Status { get; private set; }

    /// <summary>Overrides <c>Lesson.Title</c> for this specific occurrence. Empty means "use the
    /// linked lesson's title" — resolved at read time by the query handler via
    /// <c>ICourseQueryService</c>, not stored here (see ADR-006 → "Тема занятия").</summary>
    public string? Topic { get; private set; }
    public string? MeetingUrl { get; private set; }
    public string? CancelReason { get; private set; }

    /// <summary>Points at the OLD session this one replaces — set on the newly-created session by
    /// <c>RescheduleSessionCommandHandler</c>, which also flips the old session's
    /// <see cref="Status"/> to <see cref="SessionStatus.Rescheduled"/> via <see cref="MarkRescheduled"/>.</summary>
    public Guid? RescheduledFromId { get; private set; }

    /// <summary>Which <see cref="ScheduleTemplate"/> generated this row, if any — null for sessions
    /// created manually (trial lesson, make-up, consultation) or born from a reschedule.</summary>
    public Guid? ScheduleTemplateId { get; private set; }
    public string? TeacherComment { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private Session() { }

    public static Session Create(
        Guid studyGroupId,
        Guid? lessonId,
        Guid teacherId,
        Guid? roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? topic,
        string? meetingUrl,
        Guid? scheduleTemplateId = null,
        Guid? rescheduledFromId = null)
    {
        if (studyGroupId == Guid.Empty)
        {
            throw new ArgumentException("StudyGroupId is required.", nameof(studyGroupId));
        }
        if (teacherId == Guid.Empty)
        {
            throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        }
        ValidateTimes(startUtc, endUtc);

        return new Session
        {
            Id = Guid.CreateVersion7(),
            StudyGroupId = studyGroupId,
            LessonId = lessonId,
            TeacherId = teacherId,
            RoomId = roomId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Status = SessionStatus.Planned,
            Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim(),
            MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim(),
            ScheduleTemplateId = scheduleTemplateId,
            RescheduledFromId = rescheduledFromId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Update(
        Guid? lessonId,
        Guid teacherId,
        Guid? roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? topic,
        string? meetingUrl,
        string? teacherComment)
    {
        EnsureMutable();
        if (teacherId == Guid.Empty)
        {
            throw new ArgumentException("TeacherId is required.", nameof(teacherId));
        }
        ValidateTimes(startUtc, endUtc);

        LessonId = lessonId;
        TeacherId = teacherId;
        RoomId = roomId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
        MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim();
        TeacherComment = string.IsNullOrWhiteSpace(teacherComment) ? null : teacherComment.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Planned → Held. Idempotent. The caller (handler) seeds <c>Attendance</c> rows for
    /// students active in the group on the session's local date — done once, outside this method,
    /// since it needs <c>IStudyGroupQueryService</c> (see docs/02 Модули/Scheduling.md → Инварианты).</summary>
    public void Hold()
    {
        if (Status == SessionStatus.Held)
        {
            return;
        }
        if (Status is SessionStatus.Cancelled or SessionStatus.Rescheduled)
        {
            throw new CustomException(
                $"Cannot hold a session in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = SessionStatus.Held;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Planned → Cancelled. A cancelled session raises no accrual in Payments (see
    /// docs/02 Модули/Scheduling.md → Инварианты).</summary>
    public void Cancel(string? reason)
    {
        if (Status == SessionStatus.Cancelled)
        {
            return;
        }
        if (Status is SessionStatus.Held or SessionStatus.Rescheduled)
        {
            throw new CustomException(
                $"Cannot cancel a session in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = SessionStatus.Cancelled;
        CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Planned → Rescheduled — marks THIS session superseded. Called on the old session by
    /// <c>RescheduleSessionCommandHandler</c> in the same transaction that creates the replacement
    /// (whose <see cref="RescheduledFromId"/> points back at this session's <see cref="BaseEntity{TId}.Id"/>).</summary>
    public void MarkRescheduled()
    {
        if (Status == SessionStatus.Rescheduled)
        {
            return;
        }
        if (Status is SessionStatus.Held or SessionStatus.Cancelled)
        {
            throw new CustomException(
                $"Cannot reschedule a session in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        Status = SessionStatus.Rescheduled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureMutable()
    {
        if (Status is SessionStatus.Held or SessionStatus.Cancelled or SessionStatus.Rescheduled)
        {
            throw new CustomException(
                $"Cannot modify a session in status {Status}.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }
    }

    private static void ValidateTimes(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (endUtc <= startUtc)
        {
            throw new ArgumentException("EndUtc must be after StartUtc.", nameof(endUtc));
        }
    }
}
