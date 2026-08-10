using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Domain;

namespace Curriculum.Tests.Domain;

public sealed class LessonMaterialTests
{
    [Fact]
    public void Create_Should_Succeed_When_OnlyFileIdIsSet()
    {
        var material = LessonMaterial.Create(
            Guid.NewGuid(), MaterialKind.File, "Слайды", Guid.NewGuid(), null, true, 0);

        material.FileId.ShouldNotBeNull();
        material.Url.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_Succeed_When_OnlyUrlIsSet()
    {
        var material = LessonMaterial.Create(
            Guid.NewGuid(), MaterialKind.Link, "Видео", null, "https://example.com/v", true, 0);

        material.Url.ShouldBe("https://example.com/v");
        material.FileId.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_Throw_When_BothFileIdAndUrlAreSet()
    {
        Should.Throw<ArgumentException>(() =>
            LessonMaterial.Create(
                Guid.NewGuid(), MaterialKind.File, "Материал", Guid.NewGuid(), "https://example.com", true, 0));
    }

    [Fact]
    public void Create_Should_Throw_When_NeitherFileIdNorUrlIsSet()
    {
        Should.Throw<ArgumentException>(() =>
            LessonMaterial.Create(Guid.NewGuid(), MaterialKind.File, "Материал", null, null, true, 0));
    }
}
