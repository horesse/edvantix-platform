namespace FSH.Modules.Notifications.Templating;

/// <summary>
/// Stable logical keys for every notification type Edvantix raises. The value is stored verbatim in
/// <c>Notification.Type</c> (the dashboard picks an icon from it) and is also the template key in
/// <see cref="NotificationTemplateCatalog"/>. Mirrors the «Каталог уведомлений Edvantix» table in
/// <c>docs/02 Модули/Notifications.md</c>.
/// </summary>
public static class NotificationTypes
{
    // Scheduling
    public const string SessionReminder = "scheduling.session.reminder";
    public const string SessionCancelled = "scheduling.session.cancelled";
    public const string SessionRescheduled = "scheduling.session.rescheduled";
    public const string AttendanceUnexcused = "scheduling.attendance.unexcused";

    // Payments
    public const string InvoiceIssued = "payments.invoice.issued";
    public const string PaymentConfirmed = "payments.payment.confirmed";
    public const string InvoiceOverdue = "payments.invoice.overdue";

    // Study groups
    public const string EnrolledInGroup = "studygroups.enrolled";
    public const string GroupWithoutTeacher = "studygroups.without-teacher";

    // Curriculum
    public const string LessonMaterialAdded = "curriculum.lesson.material";

    // Chat
    public const string ChatMention = "chat.mention";

    // Tickets
    public const string TicketReplied = "tickets.reply";
}
