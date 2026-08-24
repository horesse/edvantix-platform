using FSH.Modules.Scheduling.Contracts;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

public sealed class AttendanceQueryService(SchedulingDbContext dbContext) : IAttendanceQueryService
{
    public async ValueTask<int> CountHeldSessionsAsync(
        Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToUtcRange(from, toDate);

        return await (
                from a in dbContext.Attendances.AsNoTracking()
                join s in dbContext.Sessions.AsNoTracking() on a.SessionId equals s.Id
                where a.StudentId == studentId
                    && s.StudyGroupId == studyGroupId
                    && s.Status == SessionStatus.Held
                    && s.StartUtc >= fromUtc && s.StartUtc <= toUtc
                select s.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask<AttendanceBreakdown> GetBreakdownAsync(
        Guid studentId, Guid studyGroupId, DateOnly from, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtc) = ToUtcRange(from, toDate);

        var statuses = await (
                from a in dbContext.Attendances.AsNoTracking()
                join s in dbContext.Sessions.AsNoTracking() on a.SessionId equals s.Id
                where a.StudentId == studentId
                    && s.StudyGroupId == studyGroupId
                    && s.StartUtc >= fromUtc && s.StartUtc <= toUtc
                select a.Status)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AttendanceBreakdown(
            Present: statuses.Count(s => s == AttendanceStatus.Present),
            Absent: statuses.Count(s => s == AttendanceStatus.Absent),
            Late: statuses.Count(s => s == AttendanceStatus.Late),
            Excused: statuses.Count(s => s == AttendanceStatus.Excused),
            Total: statuses.Count);
    }

    /// <summary>Same UTC-boundary approximation as <c>GetStudentAttendanceQueryHandler</c> — see
    /// its remarks for why exact timezone conversion isn't load-bearing here.</summary>
    private static (DateTimeOffset From, DateTimeOffset To) ToUtcRange(DateOnly from, DateOnly toDate) => (
        new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
        new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero));
}
