using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.DeleteRoom;

public sealed class DeleteRoomCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<DeleteRoomCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Room {command.RoomId} not found.");

        // No DB-level FK from Session/ScheduleTemplate to Room (see RoomConfiguration remarks), so
        // deleting a room does not require reassigning existing sessions/templates first — a stale
        // RoomId just reads as "room unset" on display.
        dbContext.Rooms.Remove(room);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
