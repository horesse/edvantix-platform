namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record AttendanceBreakdown(int Present, int Absent, int Late, int Excused, int Total);
