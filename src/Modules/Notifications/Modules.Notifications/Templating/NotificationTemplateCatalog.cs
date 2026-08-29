namespace FSH.Modules.Notifications.Templating;

/// <summary>Look-up for the built-in <see cref="NotificationTemplate"/> set, keyed by <see cref="NotificationTypes"/>.</summary>
public interface INotificationTemplateCatalog
{
    /// <summary>The template for <paramref name="key"/>. Throws <see cref="KeyNotFoundException"/> when absent.</summary>
    NotificationTemplate GetTemplate(string key);

    bool TryGetTemplate(string key, out NotificationTemplate result);
}

/// <summary>
/// The static catalogue of notification copy. One entry per row of «Каталог уведомлений Edvantix» in
/// <c>docs/02 Модули/Notifications.md</c>. Types whose channel is «приложение» only carry no e-mail
/// fields; types marked «приложение + почта» carry a subject and an HTML body.
/// </summary>
public sealed class NotificationTemplateCatalog : INotificationTemplateCatalog
{
    private static readonly IReadOnlyDictionary<string, NotificationTemplate> Templates = Build();

    public NotificationTemplate GetTemplate(string key) =>
        Templates.TryGetValue(key, out var template)
            ? template
            : throw new KeyNotFoundException($"No notification template registered for '{key}'.");

    public bool TryGetTemplate(string key, out NotificationTemplate result)
    {
        if (Templates.TryGetValue(key, out var found))
        {
            result = found;
            return true;
        }

        result = null!;
        return false;
    }

    /// <summary>All registered keys — used by tests to assert the catalogue and <see cref="NotificationTypes"/> stay in sync.</summary>
    public static IReadOnlyCollection<string> Keys => (IReadOnlyCollection<string>)Templates.Keys;

    private static IReadOnlyDictionary<string, NotificationTemplate> Build()
    {
        var templates = new NotificationTemplate[]
        {
            new(
                NotificationTypes.SessionReminder,
                TitleTemplate: "Lesson tomorrow: {{group}}",
                BodyTemplate: "{{group}} on {{date}} at {{time}}.",
                LinkTemplate: "/schedule/sessions/{{sessionId}}",
                EmailSubjectTemplate: "Reminder: {{group}} lesson on {{date}}",
                EmailHtmlBodyTemplate: Email(
                    "Lesson tomorrow",
                    "<p>This is a reminder that <strong>{{group}}</strong> has a lesson on " +
                    "<strong>{{date}}</strong> at <strong>{{time}}</strong>.</p>")),

            new(
                NotificationTypes.SessionCancelled,
                TitleTemplate: "Lesson cancelled: {{group}}",
                BodyTemplate: "The {{date}} {{time}} lesson for {{group}} was cancelled. {{reason}}",
                LinkTemplate: "/schedule/sessions/{{sessionId}}",
                EmailSubjectTemplate: "Lesson cancelled: {{group}} on {{date}}",
                EmailHtmlBodyTemplate: Email(
                    "Lesson cancelled",
                    "<p>The <strong>{{date}}</strong> {{time}} lesson for <strong>{{group}}</strong> " +
                    "has been cancelled.</p><p>{{reason}}</p>")),

            new(
                NotificationTypes.SessionRescheduled,
                TitleTemplate: "Lesson moved: {{group}}",
                BodyTemplate: "{{group}} moved from {{oldStart}} to {{newStart}}.",
                LinkTemplate: "/schedule/sessions/{{sessionId}}",
                EmailSubjectTemplate: "Lesson rescheduled: {{group}}",
                EmailHtmlBodyTemplate: Email(
                    "Lesson rescheduled",
                    "<p><strong>{{group}}</strong> has moved from <strong>{{oldStart}}</strong> " +
                    "to <strong>{{newStart}}</strong>.</p>")),

            new(
                NotificationTypes.AttendanceUnexcused,
                TitleTemplate: "Unexcused absence: {{student}}",
                BodyTemplate: "{{student}} was marked absent without an excuse for the {{date}} {{group}} lesson.",
                LinkTemplate: "/students/{{studentId}}/attendance",
                EmailSubjectTemplate: "{{student}} missed a lesson",
                EmailHtmlBodyTemplate: Email(
                    "Unexcused absence",
                    "<p><strong>{{student}}</strong> was marked absent without an excuse for the " +
                    "<strong>{{date}}</strong> <strong>{{group}}</strong> lesson.</p>")),

            new(
                NotificationTypes.InvoiceIssued,
                TitleTemplate: "Invoice {{invoiceNumber}} issued",
                BodyTemplate: "Invoice {{invoiceNumber}} for {{amount}} is due by {{dueDate}}.",
                LinkTemplate: "/billing/invoices/{{invoiceId}}",
                EmailSubjectTemplate: "Invoice {{invoiceNumber}} issued",
                EmailHtmlBodyTemplate: Email(
                    "Invoice issued",
                    "<p>Invoice <strong>{{invoiceNumber}}</strong> for <strong>{{amount}}</strong> " +
                    "has been issued and is due by <strong>{{dueDate}}</strong>.</p>")),

            new(
                NotificationTypes.PaymentConfirmed,
                TitleTemplate: "Payment received: {{amount}}",
                BodyTemplate: "Your payment of {{amount}} for invoice {{invoiceNumber}} has been confirmed.",
                LinkTemplate: "/billing/invoices/{{invoiceId}}"),

            new(
                NotificationTypes.InvoiceOverdue,
                TitleTemplate: "Invoice {{invoiceNumber}} overdue",
                BodyTemplate: "Invoice {{invoiceNumber}} for {{amount}} was due on {{dueDate}} and is now overdue.",
                LinkTemplate: "/billing/invoices/{{invoiceId}}",
                EmailSubjectTemplate: "Invoice {{invoiceNumber}} is overdue",
                EmailHtmlBodyTemplate: Email(
                    "Invoice overdue",
                    "<p>Invoice <strong>{{invoiceNumber}}</strong> for <strong>{{amount}}</strong> " +
                    "was due on <strong>{{dueDate}}</strong> and is now overdue.</p>")),

            new(
                NotificationTypes.EnrolledInGroup,
                TitleTemplate: "Enrolled in {{group}}",
                BodyTemplate: "{{student}} has been enrolled in {{group}}.",
                LinkTemplate: "/study-groups/{{studyGroupId}}",
                EmailSubjectTemplate: "{{student}} enrolled in {{group}}",
                EmailHtmlBodyTemplate: Email(
                    "Enrolment confirmed",
                    "<p><strong>{{student}}</strong> has been enrolled in <strong>{{group}}</strong>.</p>")),

            new(
                NotificationTypes.GroupWithoutTeacher,
                TitleTemplate: "{{group}} has no teacher",
                BodyTemplate: "{{group}} is active but has no teacher assigned.",
                LinkTemplate: "/study-groups/{{studyGroupId}}"),

            new(
                NotificationTypes.LessonMaterialAdded,
                TitleTemplate: "New material: {{lesson}}",
                BodyTemplate: "A new material was added to \"{{lesson}}\" in {{course}}.",
                LinkTemplate: "/courses/{{courseId}}/lessons/{{lessonId}}"),

            new(
                NotificationTypes.ChatMention,
                TitleTemplate: "You were mentioned in {{channel}}",
                BodyTemplate: "{{preview}}",
                LinkTemplate: "/chat/{{channelId}}?messageId={{messageId}}"),

            new(
                NotificationTypes.TicketReplied,
                TitleTemplate: "Reply on \"{{ticketSubject}}\"",
                BodyTemplate: "{{author}} replied to your ticket \"{{ticketSubject}}\".",
                LinkTemplate: "/support/tickets/{{ticketId}}",
                EmailSubjectTemplate: "Reply on your ticket \"{{ticketSubject}}\"",
                EmailHtmlBodyTemplate: Email(
                    "New reply on your ticket",
                    "<p><strong>{{author}}</strong> replied to your ticket " +
                    "\"<strong>{{ticketSubject}}</strong>\".</p>")),
        };

        return templates.ToDictionary(t => t.Key, StringComparer.Ordinal);
    }

    /// <summary>Wraps a body fragment in the shared plain e-mail layout (mirrors <c>BillingEmailBodies.Wrap</c>).</summary>
    private static string Email(string heading, string innerHtml) =>
        "<div style=\"font-family:Arial,Helvetica,sans-serif;font-size:14px;color:#1a1a1a;line-height:1.5\">" +
        $"<h2 style=\"font-size:18px;margin:0 0 12px\">{heading}</h2>" +
        innerHtml +
        "<p style=\"margin-top:24px;color:#6b7280;font-size:12px\">This is an automated message from your school.</p>" +
        "</div>";
}
