using FSH.Modules.Curriculum;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;
using Microsoft.Extensions.Options;

namespace Curriculum.Tests.Features;

public sealed class AddLessonMaterialCommandValidatorTests
{
    private readonly AddLessonMaterialCommandValidator _validator =
        new(Options.Create(new CurriculumOptions()));

    [Fact]
    public void Validate_Should_Pass_When_OnlyFileIdIsSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.File, "Слайды", Guid.NewGuid(), null, true);

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_OnlyUrlIsSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Link, "Ссылка", null, "https://example.com", true);

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_BothFileIdAndUrlAreSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Homework, "Материал", Guid.NewGuid(), "https://example.com", true);

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_NeitherFileIdNorUrlIsSet()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.File, "Материал", null, null, true);

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_Video_Uses_FileId()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Video, "Запись занятия", Guid.NewGuid(), null, true);

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_File_Uses_Url()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.File, "Слайды", null, "https://example.com/slides.pdf", true);

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_Video_Host_Not_Allowed()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Video, "Запись занятия", null, "https://files.example.com/lecture.mp4", true);

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("https://www.youtube.com/watch?v=abc123")]
    [InlineData("https://youtu.be/abc123")]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://rutube.ru/video/abc/")]
    public void Validate_Should_Pass_When_Video_Host_Allowed(string url)
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Video, "Запись занятия", null, url, true);

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_Link_Url_Is_Not_Absolute_Http()
    {
        var command = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Link, "Ссылка", null, "ftp://example.com/x", true);

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Respect_Configured_Hosts()
    {
        var validator = new AddLessonMaterialCommandValidator(
            Options.Create(new CurriculumOptions { VideoMaterialAllowedHosts = ["kinescope.io"] }));

        var onCustomHost = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Video, "Запись", null, "https://play.kinescope.io/abc", true);
        var onDefaultHost = new AddLessonMaterialCommand(
            Guid.NewGuid(), MaterialKind.Video, "Запись", null, "https://youtu.be/abc", true);

        validator.Validate(onCustomHost).IsValid.ShouldBeTrue();
        validator.Validate(onDefaultHost).IsValid.ShouldBeFalse();
    }
}
