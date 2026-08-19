using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.GetGroupAttendanceReport;

public sealed class GetGroupAttendanceReportQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetGroupAttendanceReportQuery, AttendanceReportDto>
{
    public async ValueTask<AttendanceReportDto> Handle(
        GetGroupAttendanceReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var fromUtc = new DateTimeOffset(query.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = new DateTimeOffset(query.To.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        var rows =
            from a in dbContext.Attendances.AsNoTracking()
            join s in dbContext.Sessions.AsNoTracking() on a.SessionId equals s.Id
            where s.StudyGroupId == query.StudyGroupId && s.StartUtc >= fromUtc && s.StartUtc <= toUtc
            select a;

        var attendance = await rows.ToListAsync(cancellationToken).ConfigureAwait(false);

        var summaries = attendance
            .GroupBy(a => a.StudentId)
            .Select(g => new StudentAttendanceSummaryDto(
                g.Key,
                PresentCount: g.Count(a => a.Status == AttendanceStatus.Present),
                AbsentCount: g.Count(a => a.Status == AttendanceStatus.Absent),
                LateCount: g.Count(a => a.Status == AttendanceStatus.Late),
                ExcusedCount: g.Count(a => a.Status == AttendanceStatus.Excused),
                TotalCount: g.Count()))
            .OrderBy(s => s.StudentId)
            .ToList();

        return new AttendanceReportDto(query.StudyGroupId, query.From, query.To, summaries);
    }
}
