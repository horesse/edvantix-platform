using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.Payments;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Features.v1.StudentInvoices;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.Payments.GetInvoicePayments;

public sealed class GetInvoicePaymentsQueryHandler(PaymentsDbContext dbContext)
    : IQueryHandler<GetInvoicePaymentsQuery, IReadOnlyList<PaymentConfirmationDto>>
{
    public async ValueTask<IReadOnlyList<PaymentConfirmationDto>> Handle(GetInvoicePaymentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var payments = await dbContext.PaymentConfirmations
            .AsNoTracking()
            .Where(p => p.InvoiceId == query.InvoiceId)
            .OrderBy(p => p.ConfirmedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return payments.Select(p => p.ToDto()).ToList();
    }
}
