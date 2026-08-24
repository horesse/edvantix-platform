using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.DeleteScheduleTemplate;

public sealed class DeleteScheduleTemplateCommandValidator : AbstractValidator<DeleteScheduleTemplateCommand>
{
    public DeleteScheduleTemplateCommandValidator()
    {
        RuleFor(x => x.ScheduleTemplateId).NotEmpty();
    }
}
