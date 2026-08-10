using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.DeleteTeacher;

public sealed class DeleteTeacherCommandValidator : AbstractValidator<DeleteTeacherCommand>
{
    public DeleteTeacherCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
