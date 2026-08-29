namespace FSH.Modules.Notifications.Templating;

/// <summary>
/// Renders a registered <see cref="NotificationTemplate"/> to a <see cref="RenderedNotification"/> by
/// substituting <c>{{token}}</c> placeholders. Integration-event handlers call this instead of
/// interpolating strings inline, so the copy for every notification type lives in one catalogue.
/// </summary>
public interface INotificationTemplateRenderer
{
    /// <summary>
    /// Render the template registered under <paramref name="templateKey"/>. Throws
    /// <see cref="KeyNotFoundException"/> when no such template exists (a deploy-time wiring bug, not
    /// a runtime data condition). A placeholder with no matching entry in <paramref name="tokens"/>
    /// renders as an empty string and is logged.
    /// </summary>
    RenderedNotification Render(string templateKey, IReadOnlyDictionary<string, string?> tokens);
}
