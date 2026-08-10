using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.CreateCourseModule;

public sealed class CreateCourseModuleCommandValidator : AbstractValidator<CreateCourseModuleCommand>
{
    public CreateCourseModuleCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
