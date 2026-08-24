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

namespace FSH.Modules.Payments.Features.v1.StudentInvoices.IssueInvoice;

public sealed class IssueInvoiceCommandHandler(
    PaymentsDbContext dbContext,
    [FromKeyedServices(typeof(PaymentsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<IssueInvoiceCommand, Unit>
{
    public async ValueTask<Unit> Handle(IssueInvoiceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invoice = await dbContext.StudentInvoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == command.InvoiceId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Invoice {command.InvoiceId} not found.");

        invoice.Issue(command.IssuedOn);

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var now = timeProvider.GetUtcNow();
        await outboxStore.AddAsync(
            new StudentInvoiceIssuedIntegrationEvent(
                Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "Payments",
                invoice.Id, invoice.StudentId, invoice.PayerGuardianId, invoice.Total, invoice.DueDate),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
