using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.Courses;

namespace FSH.Modules.Curriculum.Features.v1.Courses.ArchiveCourse;

public sealed class ArchiveCourseCommandValidator : AbstractValidator<ArchiveCourseCommand>
{
    public ArchiveCourseCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
    }
}
