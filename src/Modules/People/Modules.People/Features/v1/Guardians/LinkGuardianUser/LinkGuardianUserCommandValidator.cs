using FluentValidation;
using FSH.Modules.People.Contracts.v1.Guardians;

namespace FSH.Modules.People.Features.v1.Guardians.LinkGuardianUser;

public sealed class LinkGuardianUserCommandValidator : AbstractValidator<LinkGuardianUserCommand>
{
    public LinkGuardianUserCommandValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(64);
    }
}
