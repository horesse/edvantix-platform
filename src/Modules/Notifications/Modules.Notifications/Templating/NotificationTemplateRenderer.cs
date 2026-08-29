using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Notifications.Templating;

/// <summary>
/// Default <see cref="INotificationTemplateRenderer"/>: looks the template up in
/// <see cref="INotificationTemplateCatalog"/> and replaces every <c>{{token}}</c> with the matching
/// value. Values are HTML-escaped when substituted into the e-mail HTML body and passed through
/// unchanged everywhere else.
/// </summary>
public sealed partial class NotificationTemplateRenderer(
    INotificationTemplateCatalog catalog,
    ILogger<NotificationTemplateRenderer> logger)
    : INotificationTemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*(?<token>[A-Za-z0-9_]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    public RenderedNotification Render(string templateKey, IReadOnlyDictionary<string, string?> tokens)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentNullException.ThrowIfNull(tokens);

        var template = catalog.GetTemplate(templateKey);

        var title = Substitute(templateKey, template.TitleTemplate, tokens, htmlEscape: false)!;
        var body = Substitute(templateKey, template.BodyTemplate, tokens, htmlEscape: false);
        var link = Substitute(templateKey, template.LinkTemplate, tokens, htmlEscape: false);
        var emailSubject = Substitute(templateKey, template.EmailSubjectTemplate, tokens, htmlEscape: false);
        var emailBody = Substitute(templateKey, template.EmailHtmlBodyTemplate, tokens, htmlEscape: true);

        return new RenderedNotification(title, body, link, emailSubject, emailBody);
    }

    private string? Substitute(
        string templateKey, string? template, IReadOnlyDictionary<string, string?> tokens, bool htmlEscape)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return TokenPattern().Replace(template, match =>
        {
            var token = match.Groups["token"].Value;
            if (!tokens.TryGetValue(token, out var value))
            {
                logger.LogWarning(
                    "Notification template {TemplateKey} references token {{{{{Token}}}}} that was not supplied; rendering as empty",
                    templateKey, token);
                return string.Empty;
            }

            value ??= string.Empty;
            return htmlEscape ? HtmlEscape(value) : value;
        });
    }

    private static string HtmlEscape(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 16);
        foreach (var ch in value)
        {
            _ = ch switch
            {
                '&' => builder.Append("&amp;"),
                '<' => builder.Append("&lt;"),
                '>' => builder.Append("&gt;"),
                '"' => builder.Append("&quot;"),
                '\'' => builder.Append("&#39;"),
                _ => builder.Append(ch),
            };
        }

        return builder.ToString();
    }
}
