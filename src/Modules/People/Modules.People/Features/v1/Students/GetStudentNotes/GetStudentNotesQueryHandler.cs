using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.GetStudentNotes;

public sealed class GetStudentNotesQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<GetStudentNotesQuery, IReadOnlyList<StudentNoteDto>>
{
    public async ValueTask<IReadOnlyList<StudentNoteDto>> Handle(
        GetStudentNotesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool studentExists = await dbContext.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == query.StudentId, cancellationToken)
            .ConfigureAwait(false);
        if (!studentExists)
        {
            throw new NotFoundException($"Student {query.StudentId} not found.");
        }

        return await dbContext.StudentNotes
            .AsNoTracking()
            .Where(n => n.StudentId == query.StudentId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new StudentNoteDto(n.Id, n.StudentId, n.Text, n.AuthorUserId, n.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
