using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.ReorderLessons;

public sealed class ReorderLessonsCommandValidator : AbstractValidator<ReorderLessonsCommand>
{
    public ReorderLessonsCommandValidator()
    {
        RuleFor(x => x.CourseModuleId).NotEmpty();
        RuleFor(x => x.OrderedLessonIds).NotNull();
    }
}
