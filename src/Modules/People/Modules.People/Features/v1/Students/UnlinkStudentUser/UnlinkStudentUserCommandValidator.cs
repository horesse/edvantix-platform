using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.UnlinkStudentUser;

public sealed class UnlinkStudentUserCommandValidator : AbstractValidator<UnlinkStudentUserCommand>
{
    public UnlinkStudentUserCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
    }
}
