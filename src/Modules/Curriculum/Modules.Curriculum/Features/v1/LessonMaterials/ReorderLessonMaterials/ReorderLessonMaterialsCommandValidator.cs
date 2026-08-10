using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.ReorderLessonMaterials;

public sealed class ReorderLessonMaterialsCommandValidator : AbstractValidator<ReorderLessonMaterialsCommand>
{
    public ReorderLessonMaterialsCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.OrderedMaterialIds).NotNull();
    }
}
