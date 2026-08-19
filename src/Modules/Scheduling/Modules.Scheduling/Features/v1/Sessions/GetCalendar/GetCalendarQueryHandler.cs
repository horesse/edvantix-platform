using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.GetCalendar;

public sealed class GetCalendarQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetCalendarQuery, IReadOnlyList<CalendarEntryDto>>
{
    public async ValueTask<IReadOnlyList<CalendarEntryDto>> Handle(GetCalendarQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.StartUtc >= query.From && s.StartUtc <= query.To);

        if (query.StudyGroupId is { } studyGroupId)
        {
            q = q.Where(s => s.StudyGroupId == studyGroupId);
        }

        if (query.TeacherId is { } teacherId)
        {
            q = q.Where(s => s.TeacherId == teacherId);
        }

        if (query.RoomId is { } roomId)
        {
            q = q.Where(s => s.RoomId == roomId);
        }

        var sessions = await q.OrderBy(s => s.StartUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return sessions.Select(s => s.ToCalendarEntry()).ToList();
    }
}
