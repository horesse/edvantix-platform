using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.DeleteCourseModule;

public sealed class DeleteCourseModuleCommandValidator : AbstractValidator<DeleteCourseModuleCommand>
{
    public DeleteCourseModuleCommandValidator()
    {
        RuleFor(x => x.CourseModuleId).NotEmpty();
    }
}
