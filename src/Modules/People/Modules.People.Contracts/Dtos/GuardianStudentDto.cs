namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>The link plus a brief of the student — the guardian's-eye view of
/// <see cref="StudentGuardianDto"/>: same <see cref="Relation"/>/<see cref="IsPrimaryPayer"/>
/// per link, but carrying the ward's <see cref="Student"/> record instead of the guardian's.</summary>
public sealed record GuardianStudentDto(
    Guid Id,
    Guid StudentId,
    Guid GuardianId,
    string Relation,
    bool IsPrimaryPayer,
    StudentDto Student);
