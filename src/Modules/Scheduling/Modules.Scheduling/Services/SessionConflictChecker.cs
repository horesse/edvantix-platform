using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Services;

public sealed class SessionConflictChecker(SchedulingDbContext dbContext) : ISessionConflictChecker
{
    public async ValueTask<IReadOnlyList<SessionConflictDto>> CheckAsync(
        Guid? excludeSessionId,
        Guid teacherId,
        Guid? roomId,
        Guid studyGroupId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default)
    {
        // A cancelled/rescheduled session no longer occupies its slot — only Planned/Held count.
        var overlapping = await dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.Status == SessionStatus.Planned || s.Status == SessionStatus.Held)
            .Where(s => excludeSessionId == null || s.Id != excludeSessionId)
            .Where(s => s.StartUtc < endUtc && s.EndUtc > startUtc)
            .Where(s => s.TeacherId == teacherId || s.StudyGroupId == studyGroupId || (roomId != null && s.RoomId == roomId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        bool roomIsVirtual = roomId is null
            || await dbContext.Rooms
                .AsNoTracking()
                .Where(r => r.Id == roomId)
                .Select(r => r.IsVirtual)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

        var conflicts = new List<SessionConflictDto>();
        foreach (var s in overlapping)
        {
            if (s.TeacherId == teacherId)
            {
                conflicts.Add(new SessionConflictDto(SessionConflictType.Teacher, s.Id, s.StartUtc));
            }

            if (s.StudyGroupId == studyGroupId)
            {
                conflicts.Add(new SessionConflictDto(SessionConflictType.StudyGroup, s.Id, s.StartUtc));
            }

            if (roomId is not null && !roomIsVirtual && s.RoomId == roomId)
            {
                conflicts.Add(new SessionConflictDto(SessionConflictType.Room, s.Id, s.StartUtc));
            }
        }

        return conflicts;
    }
}
