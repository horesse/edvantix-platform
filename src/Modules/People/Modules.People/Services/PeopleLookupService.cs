using FSH.Modules.People.Contracts;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.People.Services;

public sealed class PeopleLookupService(PeopleDbContext dbContext) : IPeopleLookupService
{
    public async ValueTask<IReadOnlyDictionary<Guid, PersonBriefDto>> GetStudentsBriefAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, PersonBriefDto>();
        }

        var students = await dbContext.Students
            .AsNoTracking()
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return students.ToDictionary(s => s.Id, s => new PersonBriefDto(s.Id, s.DisplayName, s.AvatarFileId));
    }

    public async ValueTask<PersonBriefDto?> GetTeacherBriefAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var teacher = await dbContext.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return teacher is null ? null : new PersonBriefDto(teacher.Id, teacher.DisplayName, teacher.AvatarFileId);
    }

    public async ValueTask<IReadOnlyList<StudentContactsDto>> GetStudentContactsAsync(
        IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(studentIds);
        if (studentIds.Count == 0)
        {
            return [];
        }

        var students = await dbContext.Students
            .AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.LastName, s.FirstName, s.MiddleName, s.UserId, s.Email })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // One join query for every guardian of every requested student — no N+1.
        var links = await dbContext.StudentGuardians
            .AsNoTracking()
            .Where(sg => studentIds.Contains(sg.StudentId))
            .Join(
                dbContext.Guardians.AsNoTracking(),
                sg => sg.GuardianId,
                g => g.Id,
                (sg, g) => new
                {
                    sg.StudentId,
                    sg.IsPrimaryPayer,
                    g.LastName,
                    g.FirstName,
                    g.UserId,
                    g.Email,
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var guardiansByStudent = links
            .GroupBy(l => l.StudentId)
            .ToDictionary(
                grp => grp.Key,
                grp => (IReadOnlyList<ContactDto>)grp
                    .Select(l => new ContactDto(
                        NullIfBlank(l.UserId),
                        NullIfBlank(l.Email),
                        $"{l.LastName} {l.FirstName}".Trim(),
                        l.IsPrimaryPayer ? ContactRole.PrimaryPayerGuardian : ContactRole.Guardian))
                    .ToList());

        return students
            .Select(s =>
            {
                var displayName = string.IsNullOrWhiteSpace(s.MiddleName)
                    ? $"{s.LastName} {s.FirstName}"
                    : $"{s.LastName} {s.FirstName} {s.MiddleName}";

                return new StudentContactsDto(
                    s.Id,
                    displayName,
                    new ContactDto(NullIfBlank(s.UserId), NullIfBlank(s.Email), displayName, ContactRole.Student),
                    guardiansByStudent.TryGetValue(s.Id, out var g) ? g : []);
            })
            .ToList();
    }

    public async ValueTask<ContactDto?> GetTeacherContactAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        var teacher = await dbContext.Teachers
            .AsNoTracking()
            .Where(t => t.Id == teacherId)
            .Select(t => new { t.LastName, t.FirstName, t.UserId, t.Email })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return teacher is null
            ? null
            : new ContactDto(
                NullIfBlank(teacher.UserId),
                NullIfBlank(teacher.Email),
                $"{teacher.LastName} {teacher.FirstName}".Trim(),
                ContactRole.Teacher);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
