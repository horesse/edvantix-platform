using FluentValidation;
using FSH.Modules.Notifications.Contracts.v1.Commands;
using FSH.Modules.Notifications.Templating;

namespace FSH.Modules.Notifications.Features.v1.Preferences;

public sealed class UpdateNotificationPreferencesCommandValidator : AbstractValidator<UpdateNotificationPreferencesCommand>
{
    public UpdateNotificationPreferencesCommandValidator()
    {
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(item =>
            item.RuleFor(i => i.Type)
                .NotEmpty()
                .Must(t => NotificationTemplateCatalog.Keys.Contains(t))
                .WithMessage("Unknown notification type."));
    }
}
