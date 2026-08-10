namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>The link plus a brief of the guardian — richer than a bare <see cref="GuardianDto"/>
/// list, since the UI needs <see cref="Relation"/>/<see cref="IsPrimaryPayer"/> per guardian.</summary>
public sealed record StudentGuardianDto(
    Guid Id,
    Guid StudentId,
    Guid GuardianId,
    string Relation,
    bool IsPrimaryPayer,
    GuardianDto Guardian);
