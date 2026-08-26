using AutoFixture;
using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.InviteUser;
using FSH.Modules.Identity.Features.v1.Users.InviteUser;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Identity.Tests.Handlers;

/// <summary>
/// Tests for InviteUserCommandHandler — mirrors RegisterUserCommandHandlerTests /
/// ForgotPasswordCommandHandlerTests: the handler is a thin pass-through to
/// IUserService.InviteAsync, resolving the frontend origin from OriginOptions exactly like
/// ForgotPasswordCommandHandler (the accept-invite link is a dashboard SPA route, not an API one).
/// </summary>
public sealed class InviteUserCommandHandlerTests
{
    private readonly IUserService _userService;
    private readonly IOptions<OriginOptions> _originOptions;
    private readonly InviteUserCommandHandler _sut;
    private readonly IFixture _fixture;

    public InviteUserCommandHandlerTests()
    {
        _userService = Substitute.For<IUserService>();
        _originOptions = Substitute.For<IOptions<OriginOptions>>();
        _sut = new InviteUserCommandHandler(_userService, _originOptions);
        _fixture = new Fixture();
    }

    private static InviteUserCommand CreateCommand() => new()
    {
        FirstName = "Jamie",
        LastName = "Rivera",
        Email = "jamie.rivera@example.com",
        Role = "Teacher",
    };

    #region Handle - Happy Path

    [Fact]
    public async Task Handle_Should_ReturnInvitedUserId_When_InviteIsSuccessful()
    {
        // Arrange
        const string originUrl = "https://dashboard.example.com";
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri(originUrl) });
        var command = CreateCommand();
        var expectedUserId = _fixture.Create<string>();

        _userService.InviteAsync(
            command.FirstName, command.LastName, command.Email, command.Role,
            Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedUserId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(expectedUserId);
    }

    [Fact]
    public async Task Handle_Should_CallInviteAsync_WithConfiguredOrigin()
    {
        // Arrange
        const string originUrl = "https://dashboard.example.com";
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri(originUrl) });
        var command = CreateCommand();
        _userService.InviteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<string>());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert — the origin passed down is the configured frontend origin, not something derived
        // from the (permission-gated, authenticated) HTTP request that carries the invite command.
        await _userService.Received(1).InviteAsync(
            command.FirstName, command.LastName, command.Email, command.Role,
            Arg.Is<string>(o => o != null && o.StartsWith(originUrl, StringComparison.Ordinal)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_PassCancellationToken_ToUserService()
    {
        // Arrange
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri("https://dashboard.example.com") });
        var command = CreateCommand();
        using var cts = new CancellationTokenSource();
        _userService.InviteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), cts.Token)
            .Returns(_fixture.Create<string>());

        // Act
        await _sut.Handle(command, cts.Token);

        // Assert
        await _userService.Received(1).InviteAsync(
            command.FirstName, command.LastName, command.Email, command.Role,
            Arg.Any<string>(), cts.Token);
    }

    #endregion

    #region Handle - Exception Tests

    [Fact]
    public async Task Handle_Should_ThrowInvalidOperationException_When_OriginIsNotConfigured()
    {
        // Arrange
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = null });
        var command = CreateCommand();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));

        await _userService.DidNotReceive().InviteAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_PropagateException_When_UserServiceThrows()
    {
        // Arrange — e.g. duplicate e-mail, surfaced from UserRegistrationService.InviteAsync.
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri("https://dashboard.example.com") });
        var command = CreateCommand();
        var expectedMessage = "error while registering a new user";
        _userService.InviteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(expectedMessage));

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _sut.Handle(command, CancellationToken.None));
        exception.Message.ShouldBe(expectedMessage);
    }

    [Fact]
    public async Task Handle_Should_ThrowArgumentNullException_When_CommandIsNull()
    {
        _originOptions.Value.Returns(new OriginOptions { OriginUrl = new Uri("https://dashboard.example.com") });

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.Handle(null!, CancellationToken.None));
    }

    #endregion
}
