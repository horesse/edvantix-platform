using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.v1.Payments;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.Payments.ReversePayment;

public sealed class ReversePaymentCommandHandler(PaymentsDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<ReversePaymentCommand, Guid>
{
    public async ValueTask<Guid> Handle(ReversePaymentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoiceId = await dbContext.PaymentConfirmations
            .AsNoTracking()
            .Where(p => p.Id == command.PaymentId)
            .Select(p => p.InvoiceId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (invoiceId == Guid.Empty)
        {
            throw new NotFoundException($"Payment {command.PaymentId} not found.");
        }

        // Same exposure as ConfirmPayment — StudentInvoice has no row-version, so a race on the
        // invoice surfaces as a raw DbUpdateConcurrencyException. Reload-and-retry, then a clean 409.
        return await InvoiceWrite.WithConcurrencyRetryAsync(dbContext, async ct =>
        {
            var invoice = await dbContext.StudentInvoices
                .Include(i => i.Payments)
                .FirstAsync(i => i.Id == invoiceId, ct)
                .ConfigureAwait(false);

            string reversedByUserId = currentUser.GetUserId().ToString();
            var reversal = invoice.ReversePayment(command.PaymentId, reversedByUserId, command.Note);

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return reversal.Id;
        }, cancellationToken).ConfigureAwait(false);
    }
}
