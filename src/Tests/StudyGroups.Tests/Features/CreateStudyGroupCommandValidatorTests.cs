using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using FSH.Modules.StudyGroups.Features.v1.StudyGroups.CreateStudyGroup;

namespace StudyGroups.Tests.Features;

public sealed class CreateStudyGroupCommandValidatorTests
{
    private readonly CreateStudyGroupCommandValidator _validator = new();

    private static CreateStudyGroupCommand ValidCommand() => new(
        Code: "A1-01",
        Name: "English A1, group 1",
        CourseId: Guid.NewGuid(),
        PrimaryTeacherId: Guid.NewGuid(),
        Format: GroupFormat.Online,
        Capacity: 10,
        StartDate: new DateOnly(2026, 9, 1));

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_CodeIsEmpty()
    {
        var command = ValidCommand() with { Code = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_CapacityIsZero()
    {
        var command = ValidCommand() with { Capacity = 0 };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_EndDateBeforeStartDate()
    {
        var command = ValidCommand() with { EndDate = new DateOnly(2026, 8, 1) };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_PrimaryTeacherIdIsEmpty()
    {
        var command = ValidCommand() with { PrimaryTeacherId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }
}
