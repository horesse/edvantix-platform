namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record SubjectNodeDto(
    Guid Id,
    string Name,
    string Slug,
    int SortOrder,
    IReadOnlyList<SubjectNodeDto> Children);
