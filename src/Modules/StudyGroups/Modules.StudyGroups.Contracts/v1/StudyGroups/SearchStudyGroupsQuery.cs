using FSH.Framework.Shared.Persistence;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

public sealed record SearchStudyGroupsQuery(
    string? Search = null,
    Guid? CourseId = null,
    Guid? TeacherId = null,
    StudyGroupStatus? Status = null,
    GroupFormat? Format = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<StudyGroupDto>>;
