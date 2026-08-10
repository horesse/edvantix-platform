using FSH.Framework.Shared.Persistence;
using FSH.Modules.Curriculum.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Curriculum.Contracts.v1.Courses;

/// <summary>
/// Search for courses with pagination and sorting.
/// </summary>
/// <param name="Search">Search term (title / slug).</param>
/// <param name="SubjectId">Optional subject filter.</param>
/// <param name="Status">Optional status filter.</param>
/// <param name="Level">Optional level filter.</param>
/// <param name="PageNumber">Page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="SortBy">Sort column. One of: title | createdAtUtc | durationHours.</param>
/// <param name="SortDir">Sort direction. One of: asc | desc.</param>
public sealed record SearchCoursesQuery(
    string? Search = null,
    Guid? SubjectId = null,
    CourseStatus? Status = null,
    CourseLevel? Level = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null) : IQuery<PagedResponse<CourseDto>>;
