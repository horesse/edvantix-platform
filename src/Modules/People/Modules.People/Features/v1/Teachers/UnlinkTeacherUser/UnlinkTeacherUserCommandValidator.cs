using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.UnlinkTeacherUser;

public sealed class UnlinkTeacherUserCommandValidator : AbstractValidator<UnlinkTeacherUserCommand>
{
    public UnlinkTeacherUserCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
