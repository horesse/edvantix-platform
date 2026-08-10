using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.DeactivateTeacher;

public sealed class DeactivateTeacherCommandValidator : AbstractValidator<DeactivateTeacherCommand>
{
    public DeactivateTeacherCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
