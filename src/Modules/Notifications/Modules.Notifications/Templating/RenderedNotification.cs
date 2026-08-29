namespace FSH.Modules.Notifications.Templating;

/// <summary>
/// The result of rendering a <see cref="NotificationTemplate"/> with a set of tokens: ready-to-store
/// inbox text plus, when the type has an e-mail body, a ready-to-send subject and HTML body.
/// </summary>
public sealed record RenderedNotification(
    string Title,
    string? Body,
    string? Link,
    string? EmailSubject,
    string? EmailHtmlBody)
{
    /// <summary>True when both e-mail fields were produced and the type can be e-mailed.</summary>
    public bool HasEmail => EmailSubject is not null && EmailHtmlBody is not null;
}
