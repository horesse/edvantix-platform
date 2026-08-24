using FSH.Framework.Shared.Persistence;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.SearchStudentInvoices;

public sealed class SearchStudentInvoicesQueryHandler(PaymentsDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<SearchStudentInvoicesQuery, PagedResponse<StudentInvoiceDto>>
{
    public async ValueTask<PagedResponse<StudentInvoiceDto>> Handle(SearchStudentInvoicesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 50 : query.PageSize;

        var q = dbContext.StudentInvoices.AsNoTracking().AsQueryable();

        if (query.StudentId is { } studentId)
        {
            q = q.Where(i => i.StudentId == studentId);
        }

        if (query.StudyGroupId is { } studyGroupId)
        {
            q = q.Where(i => i.StudyGroupId == studyGroupId);
        }

        if (query.Status is { } status)
        {
            q = q.Where(i => i.Status == status);
        }

        if (query.PeriodFrom is { } periodFrom)
        {
            q = q.Where(i => i.PeriodTo >= periodFrom);
        }

        if (query.PeriodTo is { } periodTo)
        {
            q = q.Where(i => i.PeriodFrom <= periodTo);
        }

        if (query.HasDebt == true)
        {
            q = q.Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(i => EF.Functions.ILike(i.Number, $"%{term}%"));
        }

        q = ApplySort(q, query.SortBy, query.SortDir);

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var invoices = await q
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return new PagedResponse<StudentInvoiceDto>
        {
            Items = invoices.Select(i => i.ToDto(today)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }

    private static IQueryable<StudentInvoice> ApplySort(IQueryable<StudentInvoice> q, string? sortBy, string? sortDir)
    {
        bool desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy?.ToUpperInvariant()) switch
        {
            "DUEDATE" => desc ? q.OrderByDescending(i => i.DueDate) : q.OrderBy(i => i.DueDate),
            "TOTAL" => desc ? q.OrderByDescending(i => i.Total) : q.OrderBy(i => i.Total),
            "STATUS" => desc ? q.OrderByDescending(i => i.Status) : q.OrderBy(i => i.Status),
            _ => desc ? q.OrderByDescending(i => i.Number) : q.OrderBy(i => i.Number),
        };
    }
}
