using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Payments.Contracts.Events;
using FSH.Modules.Payments.Contracts.v1.StudentInvoices;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.CancelInvoice;

public sealed class CancelInvoiceCommandHandler(
    PaymentsDbContext dbContext,
    [FromKeyedServices(typeof(PaymentsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<CancelInvoiceCommand, Unit>
{
    public async ValueTask<Unit> Handle(CancelInvoiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // StudentInvoice has no row-version: a race on the invoice otherwise escapes as an opaque
        // DbUpdateConcurrencyException 500. Reload-and-retry, then a clean 409 if it keeps losing.
        return await InvoiceWrite.WithConcurrencyRetryAsync(dbContext, async ct =>
        {
            var invoice = await dbContext.StudentInvoices
                .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, ct)
                .ConfigureAwait(false)
                ?? throw new NotFoundException($"Invoice {command.InvoiceId} not found.");

            invoice.Cancel(command.Reason);

            var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
            var now = timeProvider.GetUtcNow();
            await outboxStore.AddAsync(
                new StudentInvoiceCancelledIntegrationEvent(
                    Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "Payments",
                    invoice.Id, command.Reason),
                ct).ConfigureAwait(false);

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            return Unit.Value;
        }, cancellationToken).ConfigureAwait(false);
    }
}
