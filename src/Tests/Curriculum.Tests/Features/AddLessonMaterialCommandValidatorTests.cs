using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;

namespace Curriculum.Tests.Features;

public sealed class AddLessonMaterialCommandValidatorTests
{
    private readonly AddLessonMaterialCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_When_OnlyFileIdIsSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.File, "Слайды", Guid.NewGuid(), null, true);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_OnlyUrlIsSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Link, "Видео", null, "https://example.com", true);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_BothFileIdAndUrlAreSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.File, "Материал", Guid.NewGuid(), "https://example.com", true);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_NeitherFileIdNorUrlIsSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.File, "Материал", null, null, true);

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }
}
