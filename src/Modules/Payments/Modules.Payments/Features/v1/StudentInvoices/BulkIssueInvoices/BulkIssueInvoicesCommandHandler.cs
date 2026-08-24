using FSH.Framework.Core.Exceptions;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkIssueInvoices;

public sealed class BulkIssueInvoicesCommandHandler(PaymentsDbContext dbContext)
    : ICommandHandler<BulkIssueInvoicesCommand, IReadOnlyList<Guid>>
{
    public async ValueTask<IReadOnlyList<Guid>> Handle(BulkIssueInvoicesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.InvoiceIds.Count == 0)
        {
            return [];
        }

        var invoices = await dbContext.StudentInvoices
            .Include(i => i.Lines)
            .Where(i => command.InvoiceIds.Contains(i.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var issued = new List<Guid>();
        foreach (var invoice in invoices)
        {
            if (invoice.Status != InvoiceStatus.Draft)
            {
                // Best-effort batch — see BulkIssueInvoicesCommand remarks: already-issued/cancelled
                // invoices in the selection are silently skipped, not treated as a failure.
                continue;
            }

            try
            {
                invoice.Issue(command.IssuedOn);
            }
            catch (CustomException)
            {
                // e.g. a Draft with no lines yet — skip it, same best-effort contract.
                continue;
            }

            issued.Add(invoice.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return issued;
    }
}
