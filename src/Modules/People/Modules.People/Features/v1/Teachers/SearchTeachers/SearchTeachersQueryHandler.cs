using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Teachers;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.People.Features.v1.Teachers.GetTeacherById;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Teachers.SearchTeachers;

public sealed class SearchTeachersQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<SearchTeachersQuery, PagedResponse<TeacherDto>>
{
    public async ValueTask<PagedResponse<TeacherDto>> Handle(SearchTeachersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.Teachers.AsNoTracking().AsQueryable();

        if (query.Status is { } status)
        {
            q = q.Where(t => t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(t =>
                EF.Functions.ILike(t.LastName, $"%{term}%") ||
                EF.Functions.ILike(t.FirstName, $"%{term}%") ||
                EF.Functions.ILike(t.Phone, $"%{term}%") ||
                EF.Functions.ILike(t.Email, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var teachers = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<TeacherDto>
        {
            Items = teachers.Select(GetTeacherByIdQueryHandler.ToDto).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static IQueryable<Teacher> ApplySort(IQueryable<Teacher> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "FIRSTNAME" => desc ? q.OrderByDescending(t => t.FirstName) : q.OrderBy(t => t.FirstName),
            "STATUS" => desc ? q.OrderByDescending(t => t.Status) : q.OrderBy(t => t.Status),
            _ => desc ? q.OrderByDescending(t => t.LastName) : q.OrderBy(t => t.LastName),
        };
    }
}
