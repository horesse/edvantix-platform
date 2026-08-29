using System.Collections.ObjectModel;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Notifications.Contracts.v1.Queries;

/// <summary>The caller's effective preference for every notification type (stored overrides merged over the defaults).</summary>
public sealed record ListNotificationPreferencesQuery : IQuery<ReadOnlyCollection<NotificationPreferenceDto>>;
