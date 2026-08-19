namespace FSH.Modules.Scheduling.Contracts.Dtos;

public sealed record GeneratedSessionPreviewDto(DateOnly LocalDate, DateTimeOffset StartUtc, DateTimeOffset EndUtc);
