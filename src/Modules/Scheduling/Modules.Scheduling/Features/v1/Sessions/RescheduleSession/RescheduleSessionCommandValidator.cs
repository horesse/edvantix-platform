using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.RescheduleSession;

public sealed class RescheduleSessionCommandValidator : AbstractValidator<RescheduleSessionCommand>
{
    public RescheduleSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.NewEndUtc).GreaterThan(x => x.NewStartUtc);
    }
}
