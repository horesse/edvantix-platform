using System.Globalization;
using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Notifications.Channels;
using FSH.Modules.Notifications.Templating;
using FSH.Modules.Payments.Contracts.Events;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>«Задолженность просрочена» → the payer, in-app + e-mail. (Manager copy is a follow-up — no manager lookup yet.)</summary>
public sealed class StudentInvoiceOverdueIntegrationEventHandler(SchoolNotificationFanout fanout)
    : IIntegrationEventHandler<StudentInvoiceOverdueIntegrationEvent>
{
    public async Task HandleAsync(StudentInvoiceOverdueIntegrationEvent @event, CancellationToken ct = default)
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
            ["amount"] = $"{@event.Debt.ToString("0.00", CultureInfo.InvariantCulture)} {@event.Currency}",
            ["daysOverdue"] = @event.DaysOverdue.ToString(CultureInfo.InvariantCulture),
            ["invoiceId"] = @event.InvoiceId.ToString(),
        };

        await fanout.DispatchAsync(
            SchoolNotificationFanout.Payers(students[0]), NotificationTypes.InvoiceOverdue, "Payments",
            NotificationChannelKind.All, tokens, @event.TenantId,
            metadata: new { invoiceId = @event.InvoiceId, studentId = @event.StudentId, daysOverdue = @event.DaysOverdue },
            ct).ConfigureAwait(false);
    }
}
