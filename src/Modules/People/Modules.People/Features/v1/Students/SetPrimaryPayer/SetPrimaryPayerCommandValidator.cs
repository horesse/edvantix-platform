using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.SetPrimaryPayer;

public sealed class SetPrimaryPayerCommandValidator : AbstractValidator<SetPrimaryPayerCommand>
{
    public SetPrimaryPayerCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}
