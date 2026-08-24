using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Features.v1.StudentInvoices;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetStudentBalance;

public sealed class GetStudentBalanceQueryHandler(PaymentsDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetStudentBalanceQuery, StudentBalanceDto>
{
    public async ValueTask<StudentBalanceDto> Handle(GetStudentBalanceQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        // Draft invoices don't count toward the balance — nothing has been billed yet. Cancelled
        // invoices never had money move (Cancel requires PaidAmount = 0), so they're moot either way.
        var invoices = await dbContext.StudentInvoices
            .AsNoTracking()
            .Where(i => i.StudentId == query.StudentId && i.Status != InvoiceStatus.Draft && i.Status != InvoiceStatus.Cancelled)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        decimal charged = invoices.Sum(i => i.Total);
        decimal paid = invoices.Sum(i => i.PaidAmount);
        decimal debt = invoices.Sum(i => Math.Max(0, i.Total - i.PaidAmount));
        decimal advance = invoices.Sum(i => Math.Max(0, i.PaidAmount - i.Total));

        var overdue = invoices
            .Where(i => i.IsOverdue(today))
            .OrderBy(i => i.DueDate)
            .Select(i => i.ToDto(today))
            .ToList();

        return new StudentBalanceDto(query.StudentId, charged, paid, debt, advance, overdue);
    }
}
