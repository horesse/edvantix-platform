using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Courses;

namespace FSH.Modules.Curriculum.Features.v1.Courses.ListTrashedCourses;

public sealed class ListTrashedCoursesQueryValidator : AbstractValidator<ListTrashedCoursesQuery>
{
    public ListTrashedCoursesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
