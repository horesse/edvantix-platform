using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.ActivateTeacher;

public sealed class ActivateTeacherCommandValidator : AbstractValidator<ActivateTeacherCommand>
{
    public ActivateTeacherCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
