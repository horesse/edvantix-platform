using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.CreateLesson;

public sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.CourseModuleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Objectives).MaximumLength(2000);
        RuleFor(x => x.Content).MaximumLength(20000);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0);
    }
}
