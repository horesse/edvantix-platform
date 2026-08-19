namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>One resource clash — <c>ISessionConflictChecker</c> (internal to the runtime project)
/// can return more than one per candidate slot, e.g. both the teacher AND the room are already
/// busy.</summary>
public sealed record SessionConflictDto(
    SessionConflictType Type,
    Guid ConflictingSessionId,
    DateTimeOffset ConflictingSessionStartUtc);
