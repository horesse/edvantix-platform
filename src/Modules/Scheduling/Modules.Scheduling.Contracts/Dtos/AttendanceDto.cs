namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record AttendanceDto(
    Guid Id,
    Guid SessionId,
    Guid StudentId,
    AttendanceStatus Status,
    string? Comment,
    string? MarkedByUserId,
    DateTimeOffset MarkedAtUtc);
