using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Courses;

namespace FSH.Modules.Curriculum.Features.v1.Courses.DuplicateCourse;

public sealed class DuplicateCourseCommandValidator : AbstractValidator<DuplicateCourseCommand>
{
    public DuplicateCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
