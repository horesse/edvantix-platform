using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using FSH.Modules.StudyGroups.Features.v1.Enrollments.ChangeEnrollmentTariff;

namespace StudyGroups.Tests.Features;

public sealed class ChangeEnrollmentTariffCommandValidatorTests
{
    private readonly ChangeEnrollmentTariffCommandValidator _validator = new();

    private static ChangeEnrollmentTariffCommand ValidCommand() => new(
        EnrollmentId: Guid.NewGuid(),
        TariffId: Guid.NewGuid(),
        DiscountPercent: 10);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        _validator.Validate(ValidCommand()).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Pass_When_TariffIdIsNull()
    {
        var command = ValidCommand() with { TariffId = null };

        _validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_EnrollmentIdIsEmpty()
    {
        var command = ValidCommand() with { EnrollmentId = Guid.Empty };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_Should_Fail_When_DiscountPercentOutOfRange(decimal percent)
    {
        var command = ValidCommand() with { DiscountPercent = percent };

        _validator.Validate(command).IsValid.ShouldBeFalse();
    }
}
