using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Courses;

namespace FSH.Modules.Curriculum.Features.v1.Courses.RestoreCourse;

public sealed class RestoreCourseCommandValidator : AbstractValidator<RestoreCourseCommand>
{
    public RestoreCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
