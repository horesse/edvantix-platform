namespace FSH.Modules.Webhooks.Contracts.Catalog;

/// <summary>
/// The canonical list of integration events a school can relay outward through a webhook
/// subscription. Kept as plain strings (no reference to the publishing modules' contract
/// assemblies) so the Webhooks module stays a leaf: the fan-out handler already matches purely on
/// <c>typeof(TEvent).Name</c>, and the strings here are those names.
///
/// This is a discovery surface, not an allow-list — a subscription may still name an event that is
/// not catalogued (forward compatibility with events added in a later release); it simply will not
/// be presented in the UI. The <c>"*"</c> wildcard subscribes to every event, catalogued or not.
/// </summary>
public static class WebhookEventCatalog
{
    /// <summary>Selector that matches every published event, catalogued or not.</summary>
    public const string Wildcard = "*";

    public static IReadOnlyList<WebhookEventType> All { get; } =
    [
        // People
        new("StudentCreatedIntegrationEvent", "People", "A student profile was created."),
        new("StudentStatusChangedIntegrationEvent", "People", "A student's status changed (e.g. active → archived)."),
        new("StudentArchivedIntegrationEvent", "People", "A student was archived."),
        new("TeacherDeactivatedIntegrationEvent", "People", "A teacher profile was deactivated."),
        new("GuardianLinkedToStudentIntegrationEvent", "People", "A guardian was linked to a student."),

        // Curriculum
        new("CoursePublishedIntegrationEvent", "Curriculum", "A course was published and is open for group creation."),
        new("CourseArchivedIntegrationEvent", "Curriculum", "A course was archived."),
        new("LessonMaterialAddedIntegrationEvent", "Curriculum", "A material was added to a lesson."),

        // StudyGroups
        new("StudyGroupCreatedIntegrationEvent", "StudyGroups", "A study group was created."),
        new("StudyGroupActivatedIntegrationEvent", "StudyGroups", "A study group was activated (session generation enabled)."),
        new("StudyGroupFinishedIntegrationEvent", "StudyGroups", "A study group finished its programme."),
        new("StudentEnrolledIntegrationEvent", "StudyGroups", "A student was enrolled into a study group."),
        new("StudentUnenrolledIntegrationEvent", "StudyGroups", "A student left / was removed from a study group."),

        // Scheduling
        new("SessionScheduledIntegrationEvent", "Scheduling", "A session was placed on the timetable."),
        new("SessionCancelledIntegrationEvent", "Scheduling", "A scheduled session was cancelled."),
        new("SessionRescheduledIntegrationEvent", "Scheduling", "A session was moved to a new time."),
        new("SessionHeldIntegrationEvent", "Scheduling", "A session was marked as held."),
        new("SessionReminderDueIntegrationEvent", "Scheduling", "A session starts in ~24 hours (reminder trigger)."),
        new("AttendanceMarkedIntegrationEvent", "Scheduling", "Attendance was recorded for a student in a session."),

        // Payments
        new("StudentInvoiceIssuedIntegrationEvent", "Payments", "An invoice was issued to a payer."),
        new("StudentInvoiceOverdueIntegrationEvent", "Payments", "An issued invoice passed its due date unpaid."),
        new("StudentInvoiceDueSoonIntegrationEvent", "Payments", "An issued invoice is approaching its due date."),
        new("StudentInvoiceCancelledIntegrationEvent", "Payments", "An invoice was cancelled / reversed."),
        new("StudentPaymentConfirmedIntegrationEvent", "Payments", "A payment against an invoice was confirmed."),
    ];

    private static readonly HashSet<string> Names =
        new(All.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);

    /// <summary>True when <paramref name="name"/> is a catalogued event type name (case-insensitive).</summary>
    public static bool Contains(string name) => Names.Contains(name);

    /// <summary>True when <paramref name="value"/> is the wildcard or a catalogued event type name.</summary>
    public static bool IsKnownSelector(string value) =>
        value == Wildcard || Names.Contains(value);
}
