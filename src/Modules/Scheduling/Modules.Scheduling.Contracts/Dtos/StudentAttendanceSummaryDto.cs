namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record StudentAttendanceSummaryDto(
    Guid StudentId,
    int PresentCount,
    int AbsentCount,
    int LateCount,
    int ExcusedCount,
    int TotalCount);
