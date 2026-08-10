using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.CreateStudent;

public sealed class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MiddleName).MaximumLength(128);
        RuleFor(x => x.BirthDate).NotEmpty().LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.ManagerUserId).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Source).MaximumLength(128);
    }
}
