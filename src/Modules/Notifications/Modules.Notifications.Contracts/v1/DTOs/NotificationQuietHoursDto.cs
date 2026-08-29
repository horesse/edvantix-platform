namespace FSH.Modules.Notifications.Contracts.v1.DTOs;

/// <summary>School-wide quiet-hours window in the school's local time. E-mail is held during the window; in-app is not.</summary>
public sealed record NotificationQuietHoursDto(bool Enabled, TimeOnly StartLocal, TimeOnly EndLocal);
