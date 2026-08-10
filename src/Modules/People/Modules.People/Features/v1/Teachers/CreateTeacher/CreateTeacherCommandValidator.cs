using FluentValidation;
using FSH.Modules.People.Contracts.v1.Teachers;

namespace FSH.Modules.People.Features.v1.Teachers.CreateTeacher;

public sealed class CreateTeacherCommandValidator : AbstractValidator<CreateTeacherCommand>
{
    public CreateTeacherCommandValidator()
    {
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.MiddleName).MaximumLength(128);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Bio).MaximumLength(2048);
        RuleFor(x => x.HourlyRate).GreaterThanOrEqualTo(0).When(x => x.HourlyRate is not null);
    }
}
