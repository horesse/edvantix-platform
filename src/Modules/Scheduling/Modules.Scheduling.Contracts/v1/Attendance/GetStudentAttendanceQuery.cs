using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Attendance;

public sealed record GetStudentAttendanceQuery(Guid StudentId, DateOnly? From, DateOnly? To) : IQuery<IReadOnlyList<AttendanceDto>>;
