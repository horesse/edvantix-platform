using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.InviteUser;
using FSH.Modules.Identity.Features.v1.Users.InviteUser;

namespace Identity.Tests.Validators;

public sealed class InviteUserCommandValidatorTests
{
    private readonly InviteUserCommandValidator _sut = new();

    private static InviteUserCommand CreateCommand(string role = SchoolRoleConstants.Teacher) => new()
    {
        FirstName = "Jamie",
        LastName = "Rivera",
        Email = "jamie.rivera@example.com",
        Role = role,
    };

    [Fact]
    public void Validate_Should_Pass_When_CommandIsValid()
    {
        var result = _sut.Validate(CreateCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_FirstNameIsEmpty(string? firstName)
    {
        var command = CreateCommand();
        command.FirstName = firstName!;

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InviteUserCommand.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_LastNameIsEmpty(string? lastName)
    {
        var command = CreateCommand();
        command.LastName = lastName!;

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InviteUserCommand.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_When_EmailIsInvalid(string? email)
    {
        var command = CreateCommand();
        command.Email = email!;

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InviteUserCommand.Email));
    }

    [Theory]
    [InlineData(SchoolRoleConstants.SchoolAdmin)]
    [InlineData(SchoolRoleConstants.Manager)]
    [InlineData(SchoolRoleConstants.Teacher)]
    [InlineData(SchoolRoleConstants.Student)]
    [InlineData(SchoolRoleConstants.Guardian)]
    public void Validate_Should_Pass_ForEverySeededSchoolRole(string role)
    {
        var result = _sut.Validate(CreateCommand(role));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("teacher")] // case-sensitive: SchoolRoleConstants values only, no casing leniency
    public void Validate_Should_Fail_When_RoleIsNotASeededSchoolRole(string? role)
    {
        var command = CreateCommand();
        command.Role = role!;

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(InviteUserCommand.Role));
    }
}
