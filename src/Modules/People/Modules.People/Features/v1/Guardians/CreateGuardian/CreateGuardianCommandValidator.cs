using FluentValidation;
using FSH.Modules.People.Contracts.v1.Guardians;

namespace FSH.Modules.People.Features.v1.Guardians.CreateGuardian;

public sealed class CreateGuardianCommandValidator : AbstractValidator<CreateGuardianCommand>
{
    public CreateGuardianCommandValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
