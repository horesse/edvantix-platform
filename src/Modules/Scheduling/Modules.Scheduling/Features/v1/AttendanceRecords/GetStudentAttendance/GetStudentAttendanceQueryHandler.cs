using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Sessions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.GetStudentAttendance;

public sealed class GetStudentAttendanceQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetStudentAttendanceQuery, IReadOnlyList<AttendanceDto>>
{
    public async ValueTask<IReadOnlyList<AttendanceDto>> Handle(
        GetStudentAttendanceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q =
            from a in dbContext.Attendances.AsNoTracking()
            join s in dbContext.Sessions.AsNoTracking() on a.SessionId equals s.Id
            where a.StudentId == query.StudentId
            select new { Attendance = a, s.StartUtc };

        // Date boundaries are treated as UTC, not converted through the school's timezone — an
        // acceptable approximation for a history filter (off by at most the UTC offset at the day's
        // edge), unlike the generator/Hold-seeding paths where local-date correctness is load-bearing.
        if (query.From is { } from)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            q = q.Where(x => x.StartUtc >= fromUtc);
        }

        if (query.To is { } to)
        {
            var toUtc = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            q = q.Where(x => x.StartUtc <= toUtc);
        }

        var rows = await q.OrderByDescending(x => x.StartUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(x => x.Attendance.ToDto()).ToList();
    }
}
