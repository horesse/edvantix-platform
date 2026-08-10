using FluentValidation;
using FSH.Modules.People.Contracts.v1.Guardians;

namespace FSH.Modules.People.Features.v1.Guardians.DeleteGuardian;

public sealed class DeleteGuardianCommandValidator : AbstractValidator<DeleteGuardianCommand>
{
    public DeleteGuardianCommandValidator()
    {
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}
