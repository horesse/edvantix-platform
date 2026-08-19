namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record AttendanceReportDto(
    Guid StudyGroupId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<StudentAttendanceSummaryDto> Students);
