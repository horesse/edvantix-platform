namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record NonWorkingDayDto(
    Guid Id,
    DateOnly Date,
    string? Description);
