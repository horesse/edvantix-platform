using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Attendance;

public sealed record GetSessionAttendanceQuery(Guid SessionId) : IQuery<IReadOnlyList<AttendanceDto>>;
