using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.SearchStudents;

public sealed class SearchStudentsQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<SearchStudentsQuery, PagedResponse<StudentDto>>
{
    public async ValueTask<PagedResponse<StudentDto>> Handle(SearchStudentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.Students.AsNoTracking().AsQueryable();

        if (query.Status is { } status)
        {
            q = q.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.ManagerUserId))
        {
            q = q.Where(s => s.ManagerUserId == query.ManagerUserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(s =>
                EF.Functions.ILike(s.LastName, $"%{term}%") ||
                EF.Functions.ILike(s.FirstName, $"%{term}%") ||
                EF.Functions.ILike(s.Phone, $"%{term}%") ||
                EF.Functions.ILike(s.Email, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var students = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<StudentDto>
        {
            Items = students.Select(ToDto).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static StudentDto ToDto(Student s) => new(
        s.Id,
        s.LastName,
        s.FirstName,
        s.MiddleName,
        s.DisplayName,
        s.BirthDate,
        s.Phone,
        s.Email,
        s.UserId,
        s.Status,
        s.Source,
        s.AvatarFileId,
        s.ManagerUserId,
        s.EnrolledAtUtc);

    private static IQueryable<Student> ApplySort(IQueryable<Student> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "FIRSTNAME" => desc ? q.OrderByDescending(s => s.FirstName) : q.OrderBy(s => s.FirstName),
            "ENROLLEDATUTC" or "ENROLLED" => desc
                ? q.OrderByDescending(s => s.EnrolledAtUtc)
                : q.OrderBy(s => s.EnrolledAtUtc),
            "STATUS" => desc ? q.OrderByDescending(s => s.Status) : q.OrderBy(s => s.Status),
            _ => desc ? q.OrderByDescending(s => s.LastName) : q.OrderBy(s => s.LastName),
        };
    }
}
