using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Rooms;

public sealed record UpdateRoomCommand(Guid RoomId, string Name, int Capacity, string? Location, bool IsVirtual) : ICommand<Unit>;
