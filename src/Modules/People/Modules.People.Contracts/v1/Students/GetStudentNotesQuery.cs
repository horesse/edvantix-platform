using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1.Students;

public sealed record GetStudentNotesQuery(Guid StudentId) : IQuery<IReadOnlyList<StudentNoteDto>>;
