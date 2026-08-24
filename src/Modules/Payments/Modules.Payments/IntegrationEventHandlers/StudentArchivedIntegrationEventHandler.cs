using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Data;
using FSH.Modules.People.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Payments.IntegrationEventHandlers;

/// <summary>"Прекратить начисления; задолженность сохраняется" (see docs/02 Модули/Payments.md →
/// «Подписки»). Cancels every outstanding <c>Draft</c> invoice for the archived student — a draft is
/// a proposed charge that was never sent, so there's nothing to preserve. Already-<c>Issued</c>/
/// <c>PartiallyPaid</c> invoices are deliberately left untouched: the debt they represent survives
/// the student's archival, exactly as the spec requires. <c>StudentInvoice.Cancel</c> only accepts
/// <c>PaidAmount = 0</c>, which a Draft trivially satisfies (it has no payments at all).</summary>
public sealed class StudentArchivedIntegrationEventHandler(
    PaymentsDbContext dbContext,
    ILogger<StudentArchivedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<StudentArchivedIntegrationEvent>
{
    public async Task HandleAsync(StudentArchivedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var drafts = await dbContext.StudentInvoices
            .Where(i => i.StudentId == @event.StudentId && i.Status == InvoiceStatus.Draft)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (drafts.Count == 0)
        {
            return;
        }

        // Draft.Cancel is a no-op-guarded state machine ("Cancel только при PaidAmount = 0") — but
        // it also refuses to cancel a *Draft* directly (Draft → Cancelled isn't a valid transition
        // for an invoice that was never issued; "delete it instead" per the domain method). Drafts
        // generated for an archived student are simply removed — they were never sent to anyone.
        dbContext.StudentInvoices.RemoveRange(drafts);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[Payments] removed {Count} draft invoice(s) for archived student {StudentId}",
                drafts.Count, @event.StudentId);
        }
    }
}
