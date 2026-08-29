namespace FSH.Modules.Notifications.Templating;

/// <summary>
/// A named, tokenised template for one notification type. One template drives both the in-app inbox
/// row (<see cref="TitleTemplate"/> / <see cref="BodyTemplate"/> / <see cref="LinkTemplate"/>) and,
/// when the type is also delivered by e-mail, the message (<see cref="EmailSubjectTemplate"/> /
/// <see cref="EmailHtmlBodyTemplate"/>).
///
/// Tokens are written <c>{{token}}</c> (whitespace inside the braces is tolerated). Values are
/// substituted verbatim into every field except <see cref="EmailHtmlBodyTemplate"/>, where they are
/// HTML-escaped — the surrounding markup is trusted, the substituted values are not.
///
/// The framework ships no template engine (see <c>BuildingBlocks/Mailing</c>), so this lives in the
/// module. It is intentionally minimal: substitution only, no conditionals or loops.
/// </summary>
public sealed record NotificationTemplate(
    string Key,
    string TitleTemplate,
    string? BodyTemplate = null,
    string? LinkTemplate = null,
    string? EmailSubjectTemplate = null,
    string? EmailHtmlBodyTemplate = null)
{
    /// <summary>True when this type is also delivered by e-mail (both e-mail fields present).</summary>
    public bool HasEmail => EmailSubjectTemplate is not null && EmailHtmlBodyTemplate is not null;
}
