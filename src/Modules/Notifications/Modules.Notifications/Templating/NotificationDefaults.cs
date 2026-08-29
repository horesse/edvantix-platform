using FSH.Modules.Notifications.Channels;

namespace FSH.Modules.Notifications.Templating;

/// <summary>
/// Default on/off per notification type when the user has set no explicit preference.
///
/// Rule (docs/02 Модули/Notifications.md → предупреждение «Уведомления — главный источник
/// раздражения»): the in-app bell shows everything by default; e-mail is opt-out only for the
/// four high-signal types (lesson cancelled / rescheduled, invoice issued / overdue) and opt-in
/// for the rest.
/// </summary>
public static class NotificationDefaults
{
    private static readonly HashSet<string> EmailOnByDefault = new(StringComparer.Ordinal)
    {
        NotificationTypes.SessionCancelled,
        NotificationTypes.SessionRescheduled,
        NotificationTypes.InvoiceIssued,
        NotificationTypes.InvoiceOverdue,
    };

    public static bool IsOn(string type, NotificationChannelKind channel) => channel switch
    {
        NotificationChannelKind.InApp => true,
        NotificationChannelKind.Email => EmailOnByDefault.Contains(type),
        _ => false,
    };
}
