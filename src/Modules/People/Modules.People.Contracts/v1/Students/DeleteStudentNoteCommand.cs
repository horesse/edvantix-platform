using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record DeleteStudentNoteCommand(Guid StudentId, Guid NoteId) : ICommand<Unit>;
