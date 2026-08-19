using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.EnrollStudents;

namespace StudyGroups.Tests.Features;

public sealed class EnrollStudentsCommandValidatorTests
{
    private readonly EnrollStudentsCommandValidator _validator = new();

    private static EnrollStudentsCommand ValidCommand() => new(
        StudyGroupId: Guid.NewGuid(),
        StudentIds: [Guid.NewGuid()]);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_StudentIdsIsEmpty()
    {
        var command = ValidCommand() with { StudentIds = [] };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_StudyGroupIdIsEmpty()
    {
        var command = ValidCommand() with { StudyGroupId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_DiscountPercentOutOfRange()
    {
        var command = ValidCommand() with { DiscountPercent = 101 };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }
}
