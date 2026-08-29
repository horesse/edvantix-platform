using FSH.Modules.Multitenancy.Contracts;

namespace FSH.Modules.Notifications.IntegrationEventHandlers;

/// <summary>Formats a UTC instant in the current school's time zone (<c>TenantSettings.TimeZoneId</c>).</summary>
public sealed class NotificationTimeFormatter(ITenantSettingsService tenantSettings)
{
    public async Task<string> ToSchoolLocalAsync(DateTimeOffset utc, CancellationToken ct = default)
    {
        var settings = await tenantSettings.GetCurrentAsync(ct).ConfigureAwait(false);

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            tz = TimeZoneInfo.Utc;
        }

        var local = TimeZoneInfo.ConvertTime(utc, tz);
        // e.g. "Mon, 05 Jan 2026 14:00"
        return local.ToString("ddd, dd MMM yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
    }
}
