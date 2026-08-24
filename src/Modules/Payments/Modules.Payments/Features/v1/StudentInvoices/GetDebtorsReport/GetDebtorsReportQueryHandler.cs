using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetDebtorsReport;

public sealed class GetDebtorsReportQueryHandler(PaymentsDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetDebtorsReportQuery, IReadOnlyList<DebtorDto>>
{
    public async ValueTask<IReadOnlyList<DebtorDto>> Handle(GetDebtorsReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var q = dbContext.StudentInvoices.AsNoTracking()
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid) && i.DueDate < today);
        if (query.StudyGroupId is { } studyGroupId)
        {
            q = q.Where(i => i.StudyGroupId == studyGroupId);
        }

        var overdue = await q.ToListAsync(cancellationToken).ConfigureAwait(false);

        return overdue
            .GroupBy(i => i.StudentId)
            .Select(g => new DebtorDto(
                g.Key,
                g.Sum(i => i.Total - i.PaidAmount),
                g.Count(),
                g.Min(i => i.DueDate)))
            .OrderByDescending(d => d.Debt)
            .ToList();
    }
}
