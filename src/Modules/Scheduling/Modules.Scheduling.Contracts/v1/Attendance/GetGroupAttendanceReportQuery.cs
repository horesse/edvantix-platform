using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.Attendance;

public sealed record GetGroupAttendanceReportQuery(Guid StudyGroupId, DateOnly From, DateOnly To) : IQuery<AttendanceReportDto>;
