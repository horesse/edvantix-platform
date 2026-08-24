using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Rooms;

public sealed record CreateRoomCommand(string Name, int Capacity, string? Location, bool IsVirtual) : ICommand<Guid>;
