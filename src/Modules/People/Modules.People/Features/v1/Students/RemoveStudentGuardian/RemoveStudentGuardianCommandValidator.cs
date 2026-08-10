using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.RemoveStudentGuardian;

public sealed class RemoveStudentGuardianCommandValidator : AbstractValidator<RemoveStudentGuardianCommand>
{
    public RemoveStudentGuardianCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}
