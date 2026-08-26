using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Users.InviteUser;

/// <summary>
/// Invites a new user by e-mail: creates the account (unconfirmed, with a random,
/// never-revealed password), assigns the requested school role, and sends a
/// set-your-password link. Narrower than <c>RegisterUserCommand</c> — no password fields,
/// since the invited user sets their own via the emailed link (accepted through the existing
/// <c>ResetPasswordCommand</c>, see docs/02 Модули/Identity.md).
/// </summary>
public class InviteUserCommand : ICommand<InviteUserResponse>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;

    /// <summary>
    /// One of <c>SchoolRoleConstants.All</c> — free-form role names are rejected by
    /// <c>InviteUserCommandValidator</c> (first-iteration scope decision, see
    /// docs/04 Задачи/Задачи · Доработки каркаса.md → Identity).
    /// </summary>
    public string Role { get; set; } = default!;
}
