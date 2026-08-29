using System.Collections.ObjectModel;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Contracts.v1.Queries;
using Mediator;

namespace FSH.Modules.Notifications.Features.v1.Preferences;

public sealed class ListNotificationPreferencesQueryHandler(
    INotificationPreferenceService preferences,
    ICurrentUser currentUser)
    : IQueryHandler<ListNotificationPreferencesQuery, ReadOnlyCollection<NotificationPreferenceDto>>
{
    public async ValueTask<ReadOnlyCollection<NotificationPreferenceDto>> Handle(
        ListNotificationPreferencesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");

        var effective = await preferences.GetEffectiveAsync(userId.ToString(), cancellationToken).ConfigureAwait(false);
        return new ReadOnlyCollection<NotificationPreferenceDto>(effective.ToList());
    }
}
