using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Features.v1.StudentInvoices;
using FSH.Modules.Payments.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.GetInvoicePdf;

public sealed class GetInvoicePdfQueryHandler(PaymentsDbContext dbContext, IInvoicePdfRenderer renderer, TimeProvider timeProvider)
    : IQueryHandler<GetInvoicePdfQuery, byte[]>
{
    public async ValueTask<byte[]> Handle(GetInvoicePdfQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var invoice = await dbContext.StudentInvoices
            .AsNoTracking()
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == query.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {query.InvoiceId} not found.");

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        return renderer.Render(invoice.ToDetailDto(today));
    }
}
