using FSH.Modules.People.Contracts.Dtos;

namespace FSH.Modules.People.Contracts;

/// <summary>
/// Batch name lookups for other modules' lists (StudyGroups rosters, Scheduling attendance,
/// Payments invoices, …) — see docs/02 Модули/People.md. Always batch
/// (<see cref="GetStudentsBriefAsync"/>), never one-id-at-a-time in a loop.
/// </summary>
public interface IPeopleLookupService
{
    ValueTask<IReadOnlyDictionary<Guid, PersonBriefDto>> GetStudentsBriefAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    ValueTask<PersonBriefDto?> GetTeacherBriefAsync(Guid id, CancellationToken cancellationToken = default);
}
