namespace FSH.Modules.Notifications.Contracts.v1.DTOs;

/// <summary>Effective on/off for one notification type, per channel (stored override or the default).</summary>
public sealed record NotificationPreferenceDto(string Type, bool InApp, bool Email);
