using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.DeleteLesson;

public sealed class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
    }
}
