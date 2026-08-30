using Stopwatch = System.Diagnostics.Stopwatch;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.GetStudentNotes;

public sealed class GetStudentNotesQueryHandler(PeopleDbContext dbContext, IAuditClient auditClient)
    : IQueryHandler<GetStudentNotesQuery, IReadOnlyList<StudentNoteDto>>
{
    public async ValueTask<IReadOnlyList<StudentNoteDto>> Handle(
        GetStudentNotesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var startedAt = Stopwatch.GetTimestamp();

        bool studentExists = await dbContext.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == query.StudentId, cancellationToken)
            .ConfigureAwait(false);
        if (!studentExists)
        {
            throw new NotFoundException($"Student {query.StudentId} not found.");
        }

        var notes = await dbContext.StudentNotes
            .AsNoTracking()
            .Where(n => n.StudentId == query.StudentId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Select(n => new StudentNoteDto(n.Id, n.StudentId, n.Text, n.AuthorUserId, n.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Internal student notes are personal data (see docs/02 Модули/Auditing.md → StudentNote).
        // Reading them changes nothing, so the EF interceptor is blind to it — audit the access
        // explicitly. Only the count is recorded, never the note text.
        await auditClient.WriteActivityAsync(
            ActivityKind.Query,
            "GetStudentNotes",
            statusCode: 200,
            durationMs: (int)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
            responsePreview: new { studentId = query.StudentId, notes = notes.Count },
            source: "People",
            ct: cancellationToken).ConfigureAwait(false);

        return notes;
    }
}
