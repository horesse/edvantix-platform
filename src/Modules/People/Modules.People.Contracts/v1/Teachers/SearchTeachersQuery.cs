using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1.Teachers;

public sealed record SearchTeachersQuery(
    string? Search = null,
    TeacherStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<TeacherDto>>;
