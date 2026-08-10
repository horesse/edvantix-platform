using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.AddStudentGuardian;

public sealed class AddStudentGuardianCommandValidator : AbstractValidator<AddStudentGuardianCommand>
{
    public AddStudentGuardianCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
        RuleFor(x => x.Relation).NotEmpty().MaximumLength(64);
    }
}
