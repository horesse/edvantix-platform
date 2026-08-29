using FSH.Modules.Notifications.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Notifications.Contracts.v1.Queries;

/// <summary>The school's quiet-hours setting (disabled by default).</summary>
public sealed record GetNotificationQuietHoursQuery : IQuery<NotificationQuietHoursDto>;
