using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.UpdateLesson;

public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Objectives).MaximumLength(2000);
        RuleFor(x => x.Content).MaximumLength(20000);
        RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0);
    }
}
