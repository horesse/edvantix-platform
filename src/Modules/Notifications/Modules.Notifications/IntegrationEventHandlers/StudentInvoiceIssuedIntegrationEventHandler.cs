using System.Globalization;
using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Payments.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Счёт выставлен» → the payer (primary-payer guardian, else the student), in-app + e-mail.</summary>
public sealed class StudentInvoiceIssuedIntegrationEventHandler(SchoolNotificationFanout fanout)
    : IIntegrationEventHandler<StudentInvoiceIssuedIntegrationEvent>
{
    public async Task HandleAsync(StudentInvoiceIssuedIntegrationEvent @event, CancellationToken ct = default)
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
            ["amount"] = $"{@event.Total.ToString("0.00", CultureInfo.InvariantCulture)} {@event.Currency}",
            ["dueDate"] = @event.DueDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
            ["invoiceId"] = @event.InvoiceId.ToString(),
        };

        await fanout.DispatchAsync(
            SchoolNotificationFanout.Payers(students[0]), NotificationTypes.InvoiceIssued, "Payments",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { invoiceId = @event.InvoiceId, studentId = @event.StudentId },
            ct).ConfigureAwait(false);
    }
}
