namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>What <c>GenerateSessionsCommand</c> actually did.</summary>
public sealed record GenerationResultDto(
    Guid ScheduleTemplateId,
    IReadOnlyList<Guid> CreatedSessionIds,
    IReadOnlyList<GenerationSkipDto> Skipped);
