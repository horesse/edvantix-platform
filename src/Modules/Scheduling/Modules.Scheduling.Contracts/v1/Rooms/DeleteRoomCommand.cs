using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Rooms;

public sealed record DeleteRoomCommand(Guid RoomId) : ICommand<Unit>;
