namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>
/// Answers "who is this Identity user in the domain" — a Student, a Teacher, a Guardian, any
/// combination (e.g. a teacher whose own child studies at the same school), or none. Consumed by
/// StudyGroups/Scheduling/Payments for row-level "is this mine" checks (see
/// <c>IPeopleScopeResolver</c> and docs/02 Модули/People.md).
/// </summary>
public sealed record PeopleScope(
    Guid? StudentId,
    Guid? TeacherId,
    Guid? GuardianId,
    IReadOnlyList<Guid> WardStudentIds)
{
    public static readonly PeopleScope Empty = new(null, null, null, []);
}
