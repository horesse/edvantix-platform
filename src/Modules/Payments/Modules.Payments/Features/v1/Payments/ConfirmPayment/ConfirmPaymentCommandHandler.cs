using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.v1.Payments;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.Payments.ConfirmPayment;

public sealed class ConfirmPaymentCommandHandler(PaymentsDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<ConfirmPaymentCommand, Guid>
{
    public async ValueTask<Guid> Handle(ConfirmPaymentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await dbContext.StudentInvoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {command.InvoiceId} not found.");

        string confirmedByUserId = currentUser.GetUserId().ToString();
        var payment = invoice.ConfirmPayment(
            command.Amount, command.PaidOn, command.Method, command.Reference, command.ProofFileId, confirmedByUserId, command.Note);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return payment.Id;
    }
}
