using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.AddStudentNote;

public sealed class AddStudentNoteCommandHandler(PeopleDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<AddStudentNoteCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddStudentNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var student = await dbContext.Students
            .Include(s => s.Notes)
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {command.StudentId} not found.");

        var note = student.AddNote(command.Text, currentUser.GetUserId().ToString());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return note.Id;
    }
}
