namespace FSH.Modules.Scheduling.Contracts.Dtos;

/// <summary>What <c>GenerateSessionsCommand</c> would do — computed without writing anything. See
/// docs/02 Модули/Scheduling.md → "Генерация".</summary>
public sealed record GenerationPreviewDto(
    Guid ScheduleTemplateId,
    IReadOnlyList<GeneratedSessionPreviewDto> ToCreate,
    IReadOnlyList<GenerationSkipDto> Skipped);
