using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.GetRooms;

public sealed class GetRoomsQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<GetRoomsQuery, IReadOnlyList<RoomDto>>
{
    public async ValueTask<IReadOnlyList<RoomDto>> Handle(GetRoomsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var rooms = await dbContext.Rooms
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rooms.Select(r => r.ToDto()).ToList();
    }
}
