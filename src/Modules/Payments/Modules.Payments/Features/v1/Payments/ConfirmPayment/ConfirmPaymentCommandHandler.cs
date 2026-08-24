using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Outbox;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Payments.Contracts.Events;
using FSH.Modules.Payments.Contracts.v1.Payments;
using FSH.Modules.Payments.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Payments.Features.v1.Payments.ConfirmPayment;

public sealed class ConfirmPaymentCommandHandler(
    PaymentsDbContext dbContext,
    ICurrentUser currentUser,
    [FromKeyedServices(typeof(PaymentsDbContext))] IOutboxStore outboxStore,
    IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
    TimeProvider timeProvider)
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

        var tenantId = multiTenantContextAccessor.MultiTenantContext.TenantInfo?.Id;
        var now = timeProvider.GetUtcNow();
        await outboxStore.AddAsync(
            new StudentPaymentConfirmedIntegrationEvent(
                Guid.NewGuid(), now.UtcDateTime, tenantId, Guid.NewGuid().ToString(), "Payments",
                invoice.Id, payment.Amount, payment.PaidOn, payment.Method),
            cancellationToken).ConfigureAwait(false);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return payment.Id;
    }
}
