using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Courses;

namespace FSH.Modules.Curriculum.Features.v1.Courses.UpdateCourse;

public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SubjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Level).IsInEnum();
        RuleFor(x => x.DurationHours).GreaterThanOrEqualTo(0);
    }
}
