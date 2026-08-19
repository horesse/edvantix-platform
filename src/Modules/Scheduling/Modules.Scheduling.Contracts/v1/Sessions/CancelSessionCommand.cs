using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

public sealed record CancelSessionCommand(Guid SessionId, string? Reason) : ICommand<Unit>;
