using FluentValidation;
using FSH.Modules.People.Contracts.v1.Students;

namespace FSH.Modules.People.Features.v1.Students.DeleteStudentNote;

public sealed class DeleteStudentNoteCommandValidator : AbstractValidator<DeleteStudentNoteCommand>
{
    public DeleteStudentNoteCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.NoteId).NotEmpty();
    }
}
