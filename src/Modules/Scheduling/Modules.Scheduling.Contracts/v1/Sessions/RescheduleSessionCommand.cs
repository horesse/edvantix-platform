using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

/// <summary>Marks the current session <c>Rescheduled</c> and creates a replacement whose
/// <c>RescheduledFromId</c> points back at it. <c>RoomId</c>/<c>TeacherId</c> null means "keep the
/// original session's value". Returns the new session's id.</summary>
public sealed record RescheduleSessionCommand(
    Guid SessionId,
    DateTimeOffset NewStartUtc,
    DateTimeOffset NewEndUtc,
    Guid? RoomId,
    Guid? TeacherId,
    bool Force) : ICommand<Guid>;
