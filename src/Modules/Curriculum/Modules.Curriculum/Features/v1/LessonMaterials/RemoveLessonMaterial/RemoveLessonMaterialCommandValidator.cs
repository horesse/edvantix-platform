using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.RemoveLessonMaterial;

public sealed class RemoveLessonMaterialCommandValidator : AbstractValidator<RemoveLessonMaterialCommand>
{
    public RemoveLessonMaterialCommandValidator()
    {
        RuleFor(x => x.MaterialId).NotEmpty();
    }
}
