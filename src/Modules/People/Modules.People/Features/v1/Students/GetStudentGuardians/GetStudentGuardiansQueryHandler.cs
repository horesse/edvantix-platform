using FSH.Framework.Core.Exceptions;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Contracts.v1.Students;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Features.v1.Students.GetStudentGuardians;

public sealed class GetStudentGuardiansQueryHandler(PeopleDbContext dbContext)
    : IQueryHandler<GetStudentGuardiansQuery, IReadOnlyList<StudentGuardianDto>>
{
    public async ValueTask<IReadOnlyList<StudentGuardianDto>> Handle(
        GetStudentGuardiansQuery query, CancellationToken cancellationToken)
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

        // Two round-trips + in-memory join rather than an EF .Join(...) projection: DisplayName
        // is a computed (non-mapped) property, and EF can't translate it inside a SQL projection.
        var links = await dbContext.StudentGuardians
            .AsNoTracking()
            .Where(l => l.StudentId == query.StudentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var guardianIds = links.Select(l => l.GuardianId).ToList();
        var guardiansById = await dbContext.Guardians
            .AsNoTracking()
            .Where(g => guardianIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, cancellationToken)
            .ConfigureAwait(false);

        return links
            .Where(l => guardiansById.ContainsKey(l.GuardianId))
            .Select(l => ToDto(l, guardiansById[l.GuardianId]))
            .ToList();
    }

    private static StudentGuardianDto ToDto(StudentGuardian link, Guardian guardian) => new(
        link.Id,
        link.StudentId,
        link.GuardianId,
        link.Relation,
        link.IsPrimaryPayer,
        new GuardianDto(guardian.Id, guardian.LastName, guardian.FirstName, guardian.DisplayName,
            guardian.Phone, guardian.Email, guardian.UserId));
}
