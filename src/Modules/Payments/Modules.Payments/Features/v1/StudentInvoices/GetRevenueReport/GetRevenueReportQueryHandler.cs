using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetRevenueReport;

public sealed class GetRevenueReportQueryHandler(PaymentsDbContext dbContext)
    : IQueryHandler<GetRevenueReportQuery, RevenueReportDto>
{
    public async ValueTask<RevenueReportDto> Handle(GetRevenueReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Net of reversals — a reversal row carries the negated amount of the payment it reverses
        // (see PaymentConfirmation.CreateReversal), so a plain sum already nets them out.
        var payments = await dbContext.PaymentConfirmations
            .AsNoTracking()
            .Where(p => p.PaidOn >= query.PeriodFrom && p.PaidOn <= query.PeriodTo)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var byMethod = payments
            .GroupBy(p => p.Method)
            .Select(g => new RevenueByMethodDto(g.Key, g.Sum(p => p.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new RevenueReportDto(query.PeriodFrom, query.PeriodTo, payments.Sum(p => p.Amount), byMethod);
    }
}
