namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record RoomDto(
    Guid Id,
    string Name,
    int Capacity,
    string? Location,
    bool IsVirtual,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
