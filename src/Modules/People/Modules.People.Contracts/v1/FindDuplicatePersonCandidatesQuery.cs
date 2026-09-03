using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1;

/// <summary>Soft duplicate check for the create-person dialogs (ученик / представитель /
/// преподаватель). Returns existing people in the tenant whose last + first name match and
/// whose phone or e-mail matches — advisory плашка only, creation is never rejected and no
/// unique index is added (see docs/04 Задачи/EDX-018 Предупреждение о дубле человека.md).
/// Backs <c>GET /api/v1/people/duplicate-candidates</c>. Not paginated: the name predicate
/// already bounds the result to a handful of rows.</summary>
public sealed record FindDuplicatePersonCandidatesQuery(
    string LastName,
    string FirstName,
    string? Phone = null,
    string? Email = null) : IQuery<IReadOnlyList<DuplicatePersonCandidateDto>>;
