using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Features.v1.Students.CreateStudent;

namespace People.Tests.Features;

/// <summary>
/// This validator is also reused verbatim by ImportStudentsCommandHandler for each CSV row
/// (see ImportStudentsCommandHandler.RowValidator) — its rules gate both single Create and
/// bulk import.
/// </summary>
public sealed class CreateStudentCommandValidatorTests
{
    private readonly CreateStudentCommandValidator _validator = new();

    private static CreateStudentCommand ValidCommand() => new(
        LastName: "Ivanova",
        FirstName: "Anna",
        MiddleName: null,
        BirthDate: new DateOnly(2012, 1, 1),
        Phone: "+15550000",
        Email: "anna@example.com",
        ManagerUserId: "manager-1",
        Source: null);

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _validator.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Should_Fail_When_LastNameIsEmpty()
    {
        var command = ValidCommand() with { LastName = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_EmailIsNotAValidAddress()
    {
        var command = ValidCommand() with { Email = "not-an-email" };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_BirthDateIsInTheFuture()
    {
        var command = ValidCommand() with { BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_When_ManagerUserIdIsEmpty()
    {
        var command = ValidCommand() with { ManagerUserId = string.Empty };

        var result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }
}
