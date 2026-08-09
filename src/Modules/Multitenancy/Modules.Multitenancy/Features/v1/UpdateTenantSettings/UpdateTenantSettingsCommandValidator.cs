using FluentValidation;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantSettings;
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
    }

    // .NET 6+ understands IANA identifiers on Windows via ICU as well as on Linux, but this must
    // still be exercised on both platforms — dev is Windows, CI/prod are Linux (see database.md).
    private static bool BeAKnownTimeZone(string timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);

    private static bool BeValidCurrencyCode(string currency) =>
        !string.IsNullOrWhiteSpace(currency) && CurrencyCodeRegex().IsMatch(currency);

    [GeneratedRegex("^[A-Za-z]{3}$")]
    private static partial Regex CurrencyCodeRegex();
}
