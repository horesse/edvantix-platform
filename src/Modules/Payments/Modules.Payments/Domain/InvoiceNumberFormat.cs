using System.Globalization;
using System.Text.RegularExpressions;

namespace FSH.Modules.Payments.Domain;

/// <summary>
/// Renders a tenant's configurable invoice-number template (<c>TenantSettings.InvoiceNumberTemplate</c>,
/// EDX-013) against a running counter and a date. Pure/stateless — the counter itself is reserved
/// concurrency-safely by <c>IInvoiceNumberGenerator</c>; this type only formats.
/// <para>
/// Supported placeholders (product decision, see docs/02 Модули/Payments.md → «Нумерация счетов»):
/// <list type="bullet">
///   <item><c>{YYYY}</c> — 4-digit year</item>
///   <item><c>{YY}</c> — 2-digit year</item>
///   <item><c>{MM}</c> — 2-digit month</item>
///   <item><c>{N}</c>…<c>{NNNNNNNNNN}</c> — the counter, left-padded with zeros to the number of
///   <c>N</c>s (a value wider than the mask is printed in full, never truncated)</item>
/// </list>
/// Any other literal text is emitted verbatim. A template that contains <c>{YYYY}</c> or <c>{YY}</c>
/// is <see cref="IsYearScoped"/> — its counter restarts at 1 each calendar year; otherwise the
/// counter runs continuously for the life of the tenant.
/// </para>
/// </summary>
public static partial class InvoiceNumberFormat
{
    /// <summary>The out-of-the-box template — year-scoped, 4-digit counter (e.g. <c>2026-0001</c>).</summary>
    public const string DefaultTemplate = "{YYYY}-{NNNN}";

    /// <summary>Max stored length — matches <c>StudentInvoice.Number</c> / <c>TenantSettings.InvoiceNumberTemplate</c>.</summary>
    public const int MaxTemplateLength = 64;

    [GeneratedRegex(@"\{(YYYY|YY|MM|N{1,10})\}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    /// <summary>
    /// True when <paramref name="template"/> carries a year placeholder, i.e. the counter is expected
    /// to reset per calendar year rather than run continuously.
    /// </summary>
    public static bool IsYearScoped(string template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return template.Contains("{YYYY}", StringComparison.Ordinal)
            || template.Contains("{YY}", StringComparison.Ordinal);
    }

    /// <summary>
    /// Validates a candidate template: non-empty, within <see cref="MaxTemplateLength"/>, only known
    /// placeholders, no stray <c>{</c>/<c>}</c>, and at least one <c>{N…}</c> counter token (without it
    /// every invoice would render the same string and collide on the unique index).
    /// </summary>
    public static bool IsValid(string? template)
    {
        if (string.IsNullOrWhiteSpace(template) || template.Length > MaxTemplateLength)
        {
            return false;
        }

        var matches = TokenRegex().Matches(template);
        var stripped = TokenRegex().Replace(template, string.Empty);
        if (stripped.Contains('{', StringComparison.Ordinal) || stripped.Contains('}', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (Match m in matches)
        {
            if (m.Value.StartsWith("{N", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Renders <paramref name="template"/> for the given <paramref name="sequence"/> counter value and
    /// <paramref name="date"/>. Tolerant of a malformed template: unknown text is kept as-is and, if no
    /// <c>{N…}</c> token is present, the sequence is appended (<c>-{0000}</c>) so numbers can never
    /// collide even when the stored template is broken.
    /// </summary>
    public static string Render(string template, long sequence, DateOnly date)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        var hasCounter = false;
        var rendered = TokenRegex().Replace(template, match =>
        {
            var token = match.Groups[1].Value;
            switch (token)
            {
                case "YYYY":
                    return date.Year.ToString("D4", CultureInfo.InvariantCulture);
                case "YY":
                    return (date.Year % 100).ToString("D2", CultureInfo.InvariantCulture);
                case "MM":
                    return date.Month.ToString("D2", CultureInfo.InvariantCulture);
                default: // run of N's
                    hasCounter = true;
                    return sequence.ToString("D" + token.Length, CultureInfo.InvariantCulture);
            }
        });

        return hasCounter
            ? rendered
            : $"{rendered}-{sequence.ToString("D4", CultureInfo.InvariantCulture)}";
    }
}
