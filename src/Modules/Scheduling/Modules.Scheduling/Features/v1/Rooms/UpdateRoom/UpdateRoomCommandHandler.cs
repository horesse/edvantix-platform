using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<UpdateRoomCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var room = await dbContext.Rooms
            .FirstOrDefaultAsync(r => r.Id == command.RoomId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Room {command.RoomId} not found.");

        room.Update(command.Name, command.Capacity, command.Location, command.IsVirtual);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
