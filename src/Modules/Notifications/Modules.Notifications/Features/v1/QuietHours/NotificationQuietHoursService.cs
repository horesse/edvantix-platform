using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Data;
using FSH.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Notifications.Features.v1.QuietHours;

/// <summary>School-wide quiet-hours window: read/write the setting and answer "is it quiet right now?".</summary>
public interface INotificationQuietHoursService
{
    Task<NotificationQuietHoursDto> GetAsync(CancellationToken ct = default);

    Task SetAsync(bool enabled, TimeOnly startLocal, TimeOnly endLocal, CancellationToken ct = default);

    /// <summary>True when the current school-local time is inside an enabled quiet-hours window.</summary>
    Task<bool> IsQuietNowAsync(CancellationToken ct = default);
}

public sealed class NotificationQuietHoursService(
    NotificationsDbContext db,
    ITenantSettingsService tenantSettings,
    TimeProvider timeProvider)
    : INotificationQuietHoursService
{
    public async Task<NotificationQuietHoursDto> GetAsync(CancellationToken ct = default)
    {
        var row = await db.NotificationQuietHours.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        return row is null
            ? new NotificationQuietHoursDto(false, new TimeOnly(21, 0), new TimeOnly(8, 0))
            : new NotificationQuietHoursDto(row.Enabled, row.StartLocal, row.EndLocal);
    }

    public async Task SetAsync(bool enabled, TimeOnly startLocal, TimeOnly endLocal, CancellationToken ct = default)
    {
        var row = await db.NotificationQuietHours.FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null)
        {
            db.NotificationQuietHours.Add(NotificationQuietHours.Create(enabled, startLocal, endLocal));
        }
        else
        {
            row.Set(enabled, startLocal, endLocal);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> IsQuietNowAsync(CancellationToken ct = default)
    {
        var row = await db.NotificationQuietHours.AsNoTracking().FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (row is null || !row.Enabled)
        {
            return false;
        }

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

        var localNow = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), tz).DateTime);
        return row.Contains(localNow);
    }
}
