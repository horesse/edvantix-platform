using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Subjects;

namespace FSH.Modules.Curriculum.Features.v1.Subjects.ReorderSubjects;

public sealed class ReorderSubjectsCommandValidator : AbstractValidator<ReorderSubjectsCommand>
{
    public ReorderSubjectsCommandValidator()
    {
        RuleFor(x => x.OrderedSubjectIds).NotNull();
    }
}
