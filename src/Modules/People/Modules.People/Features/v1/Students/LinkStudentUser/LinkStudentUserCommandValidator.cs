using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.LinkStudentUser;

public sealed class LinkStudentUserCommandValidator : AbstractValidator<LinkStudentUserCommand>
{
    public LinkStudentUserCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty().MaximumLength(64);
    }
}
