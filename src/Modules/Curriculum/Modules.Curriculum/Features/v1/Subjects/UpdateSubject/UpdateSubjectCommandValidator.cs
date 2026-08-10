using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.UpdateSubject;

public sealed class UpdateSubjectCommandValidator : AbstractValidator<UpdateSubjectCommand>
{
    public UpdateSubjectCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
