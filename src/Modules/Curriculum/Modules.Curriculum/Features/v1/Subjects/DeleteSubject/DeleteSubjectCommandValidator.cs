using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.DeleteSubject;

public sealed class DeleteSubjectCommandValidator : AbstractValidator<DeleteSubjectCommand>
{
    public DeleteSubjectCommandValidator()
    {
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}
