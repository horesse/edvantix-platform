namespace FSH.Modules.Notifications.Channels;

/// <summary>
/// What an integration-event handler hands to <see cref="INotificationDispatcher"/>: who to notify,
/// which template to render, and the tokens for it. The template is looked up by
/// <see cref="TemplateKey"/> (a <c>NotificationTypes</c> constant) and its key is also stored as the
/// notification <c>Type</c>.
/// </summary>
public sealed record NotificationRequest(
    string RecipientUserId,
    string TemplateKey,
    IReadOnlyDictionary<string, string?> Tokens)
{
    /// <summary>Originating module name stored on the inbox row (e.g. <c>Scheduling</c>) — for grouping/filtering.</summary>
    public string Source { get; init; } = "Notifications";

    /// <summary>Recipient's e-mail, when known. Required for the <see cref="NotificationChannelKind.Email"/> channel.</summary>
    public string? RecipientEmail { get; init; }

    /// <summary>Opaque metadata stored as JSON on the inbox row. Shape is owned by the caller.</summary>
    public object? Metadata { get; init; }

    /// <summary>Which channels to attempt. <see cref="NotificationChannelKind.Email"/> still only fires when the template has an e-mail body.</summary>
    public NotificationChannelKind Channels { get; init; } = NotificationChannelKind.All;

    /// <summary>
    /// When set, the dispatcher asserts the ambient tenant matches before writing. Handlers that
    /// carry the source event's <c>TenantId</c> should pass it — the DbContext captures its tenant
    /// at construction, so a publisher that forgot to establish context would leak cross-tenant.
    /// </summary>
    public string? ExpectedTenantId { get; init; }
}
