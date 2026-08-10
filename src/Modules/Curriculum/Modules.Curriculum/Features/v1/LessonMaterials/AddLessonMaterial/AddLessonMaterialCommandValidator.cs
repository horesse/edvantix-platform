using FluentValidation;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;

public sealed class AddLessonMaterialCommandValidator : AbstractValidator<AddLessonMaterialCommand>
{
    public AddLessonMaterialCommandValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Url).MaximumLength(2048);

        RuleFor(x => x)
            .Must(x => x.FileId.HasValue ^ !string.IsNullOrWhiteSpace(x.Url))
            .WithMessage("Exactly one of FileId or Url must be set.");
    }
}
