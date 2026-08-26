using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.InviteUser;
using Mediator;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Features.v1.Users.InviteUser;

public sealed class InviteUserCommandHandler : ICommandHandler<InviteUserCommand, InviteUserResponse>
{
    private readonly IUserService _userService;
    private readonly IOptions<OriginOptions> _originOptions;

    public InviteUserCommandHandler(IUserService userService, IOptions<OriginOptions> originOptions)
    {
        _userService = userService;
        _originOptions = originOptions;
    }

    public async ValueTask<InviteUserResponse> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Same origin source as ForgotPasswordCommandHandler: the accept-invite page is a
        // dashboard SPA route, so the link must point at the configured frontend origin, not
        // at whatever host served this API request (unlike RegisterUserEndpoint's confirm-email
        // link, which points back at the API itself).
        var origin = _originOptions.Value?.OriginUrl?.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new InvalidOperationException("Origin URL is not configured.");
        }

        var userId = await _userService.InviteAsync(
            command.FirstName,
            command.LastName,
            command.Email,
            command.Role,
            origin,
            cancellationToken).ConfigureAwait(false);

        return new InviteUserResponse(userId);
    }
}
