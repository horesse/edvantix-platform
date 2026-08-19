using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CreateSession;

public sealed class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.StudyGroupId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc);
        RuleFor(x => x.Topic).MaximumLength(256);
        RuleFor(x => x.MeetingUrl).MaximumLength(512);
    }
}
