using System.Globalization;
using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Payments.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Оплата подтверждена» → the payer, in-app only (catalog: no e-mail for this one).</summary>
public sealed class StudentPaymentConfirmedIntegrationEventHandler(SchoolNotificationFanout fanout)
    : IIntegrationEventHandler<StudentPaymentConfirmedIntegrationEvent>
{
    public async Task HandleAsync(StudentPaymentConfirmedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var students = await fanout.ResolveStudentsAsync(@event.StudentId, ct).ConfigureAwait(false);
        if (students.Count == 0)
        {
            return;
        }

        var tokens = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["invoiceNumber"] = @event.Number,
            ["amount"] = $"{@event.Amount.ToString("0.00", CultureInfo.InvariantCulture)} {@event.Currency}",
            ["invoiceId"] = @event.InvoiceId.ToString(),
        };

        await fanout.DispatchAsync(
            SchoolNotificationFanout.Payers(students[0]), NotificationTypes.PaymentConfirmed, "Payments",
            NotificationChannelKind.InApp, tokens, @event.TenantId,
            metadata: new { invoiceId = @event.InvoiceId, studentId = @event.StudentId },
            ct).ConfigureAwait(false);
    }
}
