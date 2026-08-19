using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Sessions;

/// <summary>Planned → Held. Seeds one <c>Attendance</c> row per student active in the group on the
/// session's local date (school timezone) — computed here, not at session-creation time, because
/// the roster can still change before the session actually happens. See
/// docs/02 Модули/Scheduling.md → Инварианты.</summary>
public sealed record HoldSessionCommand(Guid SessionId) : ICommand<Unit>;
