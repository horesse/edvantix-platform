using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.UpdateCourseModule;

public sealed class UpdateCourseModuleCommandValidator : AbstractValidator<UpdateCourseModuleCommand>
{
    public UpdateCourseModuleCommandValidator()
    {
        RuleFor(x => x.CourseModuleId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
