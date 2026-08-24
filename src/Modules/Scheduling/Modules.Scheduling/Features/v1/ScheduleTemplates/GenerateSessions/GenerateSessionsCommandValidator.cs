using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.GenerateSessions;

public sealed class GenerateSessionsCommandValidator : AbstractValidator<GenerateSessionsCommand>
{
    public GenerateSessionsCommandValidator()
    {
        RuleFor(x => x.ScheduleTemplateId).NotEmpty();
        RuleFor(x => x.HorizonWeeks).GreaterThan(0).When(x => x.HorizonWeeks is not null);
    }
}
