namespace FSH.Modules.Curriculum.Contracts.Dtos;

public sealed record SubjectDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    int SortOrder);
