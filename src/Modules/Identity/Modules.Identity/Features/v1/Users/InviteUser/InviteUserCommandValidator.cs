using FluentValidation;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.v1.Users.InviteUser;

namespace FSH.Modules.Identity.Features.v1.Users.InviteUser;

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        // First-iteration scope decision (docs/04 Задачи/Задачи · Доработки каркаса.md →
        // Identity): only the seeded school roles can be invited into — no free-form role name.
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => SchoolRoleConstants.All.Contains(role))
            .WithMessage($"Role must be one of: {string.Join(", ", SchoolRoleConstants.All)}.");
    }
}
