using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Sessions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.GetSessionAttendance;

public sealed class GetSessionAttendanceQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetSessionAttendanceQuery, IReadOnlyList<AttendanceDto>>
{
    public async ValueTask<IReadOnlyList<AttendanceDto>> Handle(
        GetSessionAttendanceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var attendance = await dbContext.Attendances
            .AsNoTracking()
            .Where(a => a.SessionId == query.SessionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return attendance.Select(a => a.ToDto()).ToList();
    }
}
