using FSH.Framework.Shared.Persistence;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.People.Features.v1.Guardians.GetGuardianById;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Guardians.SearchGuardians;

public sealed class SearchGuardiansQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<SearchGuardiansQuery, PagedResponse<GuardianDto>>
{
    public async ValueTask<PagedResponse<GuardianDto>> Handle(SearchGuardiansQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.Guardians.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(g =>
                EF.Functions.ILike(g.LastName, $"%{term}%") ||
                EF.Functions.ILike(g.FirstName, $"%{term}%") ||
                EF.Functions.ILike(g.Phone, $"%{term}%") ||
                EF.Functions.ILike(g.Email, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var guardians = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<GuardianDto>
        {
            Items = guardians.Select(GetGuardianByIdQueryHandler.ToDto).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static IQueryable<Guardian> ApplySort(IQueryable<Guardian> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "FIRSTNAME" => desc ? q.OrderByDescending(g => g.FirstName) : q.OrderBy(g => g.FirstName),
            _ => desc ? q.OrderByDescending(g => g.LastName) : q.OrderBy(g => g.LastName),
        };
    }
}
