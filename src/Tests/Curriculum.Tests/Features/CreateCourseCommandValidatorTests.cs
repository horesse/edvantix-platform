using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using FSH.Modules.Curriculum.Features.v1.Courses.CreateCourse;

namespace Curriculum.Tests.Features;

public sealed class CreateCourseCommandValidatorTests
{
    private readonly CreateCourseCommandValidator _validator = new();

    private static CreateCourseCommand ValidCommand() => new(
        SubjectId: Guid.NewGuid(),
        Title: "English A1",
        Description: "Beginner course",
        Level: CourseLevel.Beginner,
        DurationHours: 40,
        CoverFileId: null);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_TitleIsEmpty()
    {
        var command = ValidCommand() with { Title = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_SubjectIdIsEmpty()
    {
        var command = ValidCommand() with { SubjectId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_DurationHoursIsNegative()
    {
        var command = ValidCommand() with { DurationHours = -1 };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }
}
