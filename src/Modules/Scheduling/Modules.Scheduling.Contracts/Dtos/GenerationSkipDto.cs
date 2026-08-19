namespace FSH.Modules.Scheduling.Contracts.Dtos;

public enum GenerationSkipReason
{
    NonWorkingDay,
    Conflict,
}

/// <summary>One occurrence the generator did not create. <paramref name="Conflicts"/> is populated
/// only when <paramref name="Reason"/> is <see cref="GenerationSkipReason.Conflict"/>.</summary>
public sealed record GenerationSkipDto(
    DateOnly LocalDate,
    GenerationSkipReason Reason,
    IReadOnlyList<SessionConflictDto> Conflicts);
