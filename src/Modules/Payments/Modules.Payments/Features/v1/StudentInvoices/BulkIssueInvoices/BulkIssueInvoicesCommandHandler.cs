using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Contracts.Events;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.BulkIssueInvoices;

public sealed class BulkIssueInvoicesCommandHandler(
    PaymentsDbContext dbContext,
    [FromKeyedServices(typeof(PaymentsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
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

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var now = timeProvider.GetUtcNow();

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

            await outboxStore.AddAsync(
                new StudentInvoiceIssuedIntegrationEvent(
                    Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "Payments",
                    invoice.Id, invoice.StudentId, invoice.PayerGuardianId, invoice.Total, invoice.DueDate),
                cancellationToken).ConfigureAwait(false);

            issued.Add(invoice.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return issued;
    }
}
