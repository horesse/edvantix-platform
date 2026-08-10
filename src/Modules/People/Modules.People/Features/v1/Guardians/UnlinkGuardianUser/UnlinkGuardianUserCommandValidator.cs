using FluentValidation;
using FSH.Modules.People.Contracts.v1.Guardians;

namespace FSH.Modules.People.Features.v1.Guardians.UnlinkGuardianUser;

public sealed class UnlinkGuardianUserCommandValidator : AbstractValidator<UnlinkGuardianUserCommand>
{
    public UnlinkGuardianUserCommandValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}
