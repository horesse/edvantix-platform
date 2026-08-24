using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.UpdateScheduleTemplate;

public sealed class UpdateScheduleTemplateCommandValidator : AbstractValidator<UpdateScheduleTemplateCommand>
{
    public UpdateScheduleTemplateCommandValidator()
    {
        RuleFor(x => x.ScheduleTemplateId).NotEmpty();
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidTo is not null);
    }
}
