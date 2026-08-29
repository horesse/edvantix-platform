using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using Mediator;

namespace FSH.Modules.Notifications.Features.v1.Preferences;

public sealed class UpdateNotificationPreferencesCommandHandler(
    INotificationPreferenceService preferences,
    ICurrentUser currentUser)
    : ICommandHandler<UpdateNotificationPreferencesCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateNotificationPreferencesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");

        await preferences.UpsertAsync(userId.ToString(), command.Items, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
