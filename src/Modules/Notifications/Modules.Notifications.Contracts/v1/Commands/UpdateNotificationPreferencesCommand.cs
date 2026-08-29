using System.Collections.ObjectModel;
using Mediator;

namespace FSH.Modules.Notifications.Contracts.v1.Commands;

/// <summary>
/// Upserts the caller's notification preferences. Each item sets both channel toggles for one
/// <c>NotificationTypes</c> key; types not listed keep whatever they had (stored override or default).
/// </summary>
public sealed record UpdateNotificationPreferencesCommand(Collection<NotificationPreferenceItem> Items) : ICommand<Unit>;

public sealed record NotificationPreferenceItem(string Type, bool InApp, bool Email);
