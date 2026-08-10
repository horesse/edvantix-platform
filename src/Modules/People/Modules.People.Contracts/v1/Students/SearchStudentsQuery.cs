using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

/// <summary>Filters: status, manager, free-text (last/first name, phone, email).
/// Group/долг filters land once StudyGroups/Payments exist (see docs/02 Модули/People.md).</summary>
public sealed record SearchStudentsQuery(
    string? Search = null,
    StudentStatus? Status = null,
    string? ManagerUserId = null,
    int PageNumber = 1,
    int PageSize = 50,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<StudentDto>>;
