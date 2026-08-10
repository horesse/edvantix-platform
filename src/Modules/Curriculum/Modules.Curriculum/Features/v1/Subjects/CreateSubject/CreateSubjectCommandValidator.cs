using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.CreateSubject;

public sealed class CreateSubjectCommandValidator : AbstractValidator<CreateSubjectCommand>
{
    public CreateSubjectCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
