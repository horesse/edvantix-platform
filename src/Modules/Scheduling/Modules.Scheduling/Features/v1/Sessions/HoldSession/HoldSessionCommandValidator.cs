using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.HoldSession;

public sealed class HoldSessionCommandValidator : AbstractValidator<HoldSessionCommand>
{
    public HoldSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
