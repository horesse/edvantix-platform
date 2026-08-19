using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.UpdateSession;

public sealed class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc);
        RuleFor(x => x.Topic).MaximumLength(256);
        RuleFor(x => x.MeetingUrl).MaximumLength(512);
        RuleFor(x => x.TeacherComment).MaximumLength(2000);
    }
}
