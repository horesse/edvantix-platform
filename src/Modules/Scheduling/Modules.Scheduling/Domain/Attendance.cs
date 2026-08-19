using FSH.Framework.Core.Domain;
using FSH.Modules.Scheduling.Contracts.Dtos;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// One student's attendance record for one <see cref="Session"/>. Seeded (not created ad hoc) when a
/// session is held, one row per student active in the group on that date (see
/// <see cref="Session.Hold"/> and docs/02 Модули/Scheduling.md → Инварианты). Defaults to
/// <see cref="AttendanceStatus.Present"/> at creation — the manager/teacher then marks the
/// exceptions (absences/lateness), which is the smaller edit for a typical class.
/// </summary>
public sealed class Attendance : BaseEntity<Guid>
{
    public Guid SessionId { get; private set; }
    public Guid StudentId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public string? Comment { get; private set; }
    public string? MarkedByUserId { get; private set; }
    public DateTimeOffset MarkedAtUtc { get; private set; }

    private Attendance() { }

    public static Attendance CreateDefault(Guid sessionId, Guid studentId)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException("SessionId is required.", nameof(sessionId));
        }
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("StudentId is required.", nameof(studentId));
        }

        return new Attendance
        {
            Id = Guid.CreateVersion7(),
            SessionId = sessionId,
            StudentId = studentId,
            Status = AttendanceStatus.Present,
            MarkedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Mark(AttendanceStatus status, string? comment, string? markedByUserId)
    {
        Status = status;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        MarkedByUserId = markedByUserId;
        MarkedAtUtc = DateTimeOffset.UtcNow;
    }
}
