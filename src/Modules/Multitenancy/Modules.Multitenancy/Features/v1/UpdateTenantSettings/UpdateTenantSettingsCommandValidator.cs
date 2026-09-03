using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;
using System.Linq;
using System.Text.RegularExpressions;

namespace FSH.Modules.Multitenancy.Features.v1.UpdateTenantSettings;

public partial class UpdateTenantSettingsCommandValidator : AbstractValidator<UpdateTenantSettingsCommand>
{
    public UpdateTenantSettingsCommandValidator()
    {
        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .Must(BeAKnownTimeZone)
            .WithMessage("TimeZoneId must be a valid IANA time zone identifier.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(BeValidCurrencyCode)
            .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. USD).");

        // EDX-015 — grace window for the materials-on-debt block. Cap at 90 days: past that it is
        // effectively "never block", which is what the flag itself is for.
        RuleFor(x => x.DebtGraceDays)
            .InclusiveBetween(0, 90)
            .WithMessage("DebtGraceDays must be between 0 and 90.");

        // EDX-013 — only validated when supplied (null = keep current). Mirrors
        // Payments' InvoiceNumberFormat.IsValid: known placeholders only, no stray braces, and at
        // least one {N…} counter token so rendered numbers can't collide.
        RuleFor(x => x.InvoiceNumberTemplate!)
            .MaximumLength(64)
            .Must(BeAValidInvoiceNumberTemplate)
            .WithMessage("InvoiceNumberTemplate may use only {YYYY} {YY} {MM} {N…} and must contain a {N…} counter.")
            .When(x => x.InvoiceNumberTemplate is not null);
    }

    private static bool BeAValidInvoiceNumberTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        var matches = InvoiceNumberTemplateTokenRegex().Matches(template);
        var stripped = InvoiceNumberTemplateTokenRegex().Replace(template, string.Empty);
        if (stripped.Contains('{', StringComparison.Ordinal) || stripped.Contains('}', StringComparison.Ordinal))
        {
            return false;
        }

        return matches.Any(m => m.Value.StartsWith("{N", StringComparison.Ordinal));
    }

    [GeneratedRegex(@"\{(YYYY|YY|MM|N{1,10})\}", RegexOptions.CultureInvariant)]
    private static partial Regex InvoiceNumberTemplateTokenRegex();

    // .NET 6+ understands IANA identifiers on Windows via ICU as well as on Linux, but this must
    // still be exercised on both platforms — dev is Windows, CI/prod are Linux (see database.md).
    private static bool BeAKnownTimeZone(string timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    private static bool BeValidCurrencyCode(string currency) =>
        !string.IsNullOrWhiteSpace(currency) && CurrencyCodeRegex().IsMatch(currency);

    [GeneratedRegex("^[A-Za-z]{3}$")]
    private static partial Regex CurrencyCodeRegex();
}
