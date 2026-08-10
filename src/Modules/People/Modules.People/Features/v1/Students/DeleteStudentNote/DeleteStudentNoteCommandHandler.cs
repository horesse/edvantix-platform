using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.DeleteStudentNote;

public sealed class DeleteStudentNoteCommandHandler(PeopleDbContext dbContext)
    : ICommandHandler<DeleteStudentNoteCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteStudentNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var note = await dbContext.StudentNotes
            .FirstOrDefaultAsync(
                n => n.Id == command.NoteId && n.StudentId == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Note {command.NoteId} not found on student {command.StudentId}.");

        // No aggregate invariant depends on sibling notes, so this is removed directly (not via
        // Student.Notes) — the interceptor still turns it into a soft-delete update on save.
        dbContext.StudentNotes.Remove(note);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
