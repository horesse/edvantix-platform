namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record AttendanceMarkDto(Guid StudentId, AttendanceStatus Status, string? Comment);
