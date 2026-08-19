using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Sessions;

namespace FSH.Modules.Scheduling.Features.v1.Sessions.CancelSession;

public sealed class CancelSessionCommandValidator : AbstractValidator<CancelSessionCommand>
{
    public CancelSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
