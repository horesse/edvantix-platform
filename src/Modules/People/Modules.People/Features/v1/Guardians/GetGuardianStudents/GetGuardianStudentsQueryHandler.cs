using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Guardians;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Guardians.GetGuardianStudents;

public sealed class GetGuardianStudentsQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<GetGuardianStudentsQuery, IReadOnlyList<GuardianStudentDto>>
{
    public async ValueTask<IReadOnlyList<GuardianStudentDto>> Handle(
        GetGuardianStudentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool guardianExists = await dbContext.Guardians
            .AsNoTracking()
            .AnyAsync(g => g.Id == query.GuardianId, cancellationToken)
            .ConfigureAwait(false);
        if (!guardianExists)
        {
            throw new NotFoundException($"Guardian {query.GuardianId} not found.");
        }

        // Two round-trips + in-memory join rather than an EF .Join(...) projection: DisplayName
        // is a computed (non-mapped) property, and EF can't translate it inside a SQL projection.
        // Mirrors GetStudentGuardiansQueryHandler, from the guardian's side.
        var links = await dbContext.StudentGuardians
            .AsNoTracking()
            .Where(l => l.GuardianId == query.GuardianId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var studentIds = links.Select(l => l.StudentId).ToList();
        var studentsById = await dbContext.Students
            .AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken)
            .ConfigureAwait(false);

        return links
            .Where(l => studentsById.ContainsKey(l.StudentId))
            .Select(l => ToDto(l, studentsById[l.StudentId]))
            .ToList();
    }

    private static GuardianStudentDto ToDto(StudentGuardian link, Student student) => new(
        link.Id,
        link.StudentId,
        link.GuardianId,
        link.Relation,
        link.IsPrimaryPayer,
        new StudentDto(student.Id, student.LastName, student.FirstName, student.MiddleName,
            student.DisplayName, student.BirthDate, student.Phone, student.Email, student.UserId,
            student.Status, student.Source, student.AvatarFileId, student.ManagerUserId,
            student.EnrolledAtUtc));
}
