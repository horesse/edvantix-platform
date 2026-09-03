namespace FSH.Modules.People.Contracts.Dtos;

/// <summary>One possible duplicate surfaced by <c>FindDuplicatePersonCandidatesQuery</c>: an
/// existing person in the same tenant whose last + first name match the one being entered and
/// whose phone or e-mail also matches. Advisory only — creation is never blocked (see
/// docs/04 Задачи/EDX-018 Предупреждение о дубле человека.md). <see cref="PersonType"/> is one
/// of <c>"Student"</c> / <c>"Teacher"</c> / <c>"Guardian"</c>; the frontend routes the card link
/// off it.</summary>
public sealed record DuplicatePersonCandidateDto(
    Guid Id,
    string PersonType,
    string DisplayName,
    string Phone,
    string Email,
    bool PhoneMatches,
    bool EmailMatches);
