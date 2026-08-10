using FSH.Framework.Shared.Persistence;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Curriculum.Features.v1.Courses.SearchCourses;

public sealed class SearchCoursesQueryHandler(CurriculumDbContext dbContext)
    : IQueryHandler<SearchCoursesQuery, PagedResponse<CourseDto>>
{
    public async ValueTask<PagedResponse<CourseDto>> Handle(SearchCoursesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        var q = dbContext.Courses.AsNoTracking().AsQueryable();

        if (query.SubjectId is { } subjectId)
        {
            q = q.Where(c => c.SubjectId == subjectId);
        }

        if (query.Status is { } status)
        {
            q = q.Where(c => c.Status == status);
        }

        if (query.Level is { } level)
        {
            q = q.Where(c => c.Level == level);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(c =>
                EF.Functions.ILike(c.Title, $"%{term}%") ||
                EF.Functions.ILike(c.Slug, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var courses = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<CourseDto>
        {
            Items = courses.Select(c => c.ToDto()).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static IQueryable<Course> ApplySort(IQueryable<Course> q, string? sortBy, string? sortDir)
    {
        bool desc = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "TITLE" => desc ? q.OrderByDescending(c => c.Title) : q.OrderBy(c => c.Title),
            "DURATIONHOURS" => desc
                ? q.OrderByDescending(c => c.DurationHours)
                : q.OrderBy(c => c.DurationHours),
            _ => desc
                ? q.OrderByDescending(c => c.CreatedAtUtc)
                : q.OrderBy(c => c.CreatedAtUtc),
        };
    }
}
