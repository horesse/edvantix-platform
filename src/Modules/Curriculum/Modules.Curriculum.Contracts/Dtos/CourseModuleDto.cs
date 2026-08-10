namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record CourseModuleDto(
    Guid Id,
    Guid CourseId,
    string Title,
    string? Description,
    int SortOrder);
