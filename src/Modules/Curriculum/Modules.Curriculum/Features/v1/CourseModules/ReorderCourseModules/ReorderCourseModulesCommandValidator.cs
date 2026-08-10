using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.ReorderCourseModules;

public sealed class ReorderCourseModulesCommandValidator : AbstractValidator<ReorderCourseModulesCommand>
{
    public ReorderCourseModulesCommandValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.OrderedModuleIds).NotNull();
    }
}
