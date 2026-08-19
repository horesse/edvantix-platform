using FSH.Modules.Scheduling.Contracts.v1.Rooms;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Rooms.CreateRoom;

public sealed class CreateRoomCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<CreateRoomCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateRoomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var room = Room.Create(command.Name, command.Capacity, command.Location, command.IsVirtual);

        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return room.Id;
    }
}
