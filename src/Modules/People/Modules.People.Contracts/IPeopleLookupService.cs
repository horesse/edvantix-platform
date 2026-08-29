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

    /// <summary>
    /// Resolves, per student, the people who should be notified about things that happen to them:
    /// the student's own account (<see cref="StudentContactsDto.Student"/> — <see cref="ContactDto.UserId"/>
    /// null when there is no login) and each active guardian, with the primary payer flagged.
    /// Used by Notifications (recipient fan-out) and Chat (study-group channel membership fallback
    /// when the student has no account). Batched — pass every id at once.
    /// </summary>
    ValueTask<IReadOnlyList<StudentContactsDto>> GetStudentContactsAsync(
        IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken = default);

    /// <summary>The teacher as a notification/chat target, or null if unknown.</summary>
    ValueTask<ContactDto?> GetTeacherContactAsync(Guid teacherId, CancellationToken cancellationToken = default);
}
