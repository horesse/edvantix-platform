using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.RestoreStudent;

public sealed class RestoreStudentCommandValidator : AbstractValidator<RestoreStudentCommand>
{
    public RestoreStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
    }
}
