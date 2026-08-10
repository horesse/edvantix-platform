using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.LinkTeacherUser;

public sealed class LinkTeacherUserCommandValidator : AbstractValidator<LinkTeacherUserCommand>
{
    public LinkTeacherUserCommandValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(64);
    }
}
