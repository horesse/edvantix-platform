using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.AddStudentNote;

public sealed class AddStudentNoteCommandValidator : AbstractValidator<AddStudentNoteCommand>
{
    public AddStudentNoteCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
    }
}
