using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Attendance;

/// <summary>Bulk mark — one call covers the whole session's roster. Rows not already seeded by
/// <c>HoldSessionCommand</c> (e.g. a student added after the session was held) are created on the
/// fly.</summary>
public sealed record MarkAttendanceCommand(Guid SessionId, IReadOnlyList<AttendanceMarkDto> Marks) : ICommand<Unit>;
