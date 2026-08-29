using FluentValidation;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Contracts.v1.DTOs;
using FSH.Modules.Notifications.Contracts.v1.Queries;
using Mediator;

namespace FSH.Modules.Notifications.Features.v1.QuietHours;

public sealed class GetNotificationQuietHoursQueryHandler(INotificationQuietHoursService service)
    : IQueryHandler<GetNotificationQuietHoursQuery, NotificationQuietHoursDto>
{
    public async ValueTask<NotificationQuietHoursDto> Handle(GetNotificationQuietHoursQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await service.GetAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class SetNotificationQuietHoursCommandHandler(INotificationQuietHoursService service)
    : ICommandHandler<SetNotificationQuietHoursCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetNotificationQuietHoursCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await service.SetAsync(command.Enabled, command.StartLocal, command.EndLocal, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public sealed class SetNotificationQuietHoursCommandValidator : AbstractValidator<SetNotificationQuietHoursCommand>
{
    public SetNotificationQuietHoursCommandValidator()
    {
        // A window is only meaningful with two distinct times; Start > End is allowed (spans midnight).
        RuleFor(x => x)
            .Must(c => !c.Enabled || c.StartLocal != c.EndLocal)
            .WithMessage("Quiet-hours start and end must differ when enabled.");
    }
}
