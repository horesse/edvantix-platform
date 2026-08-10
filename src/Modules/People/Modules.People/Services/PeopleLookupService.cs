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
}
