using FSH.Modules.People.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.People.Contracts.v1.Guardians;

public sealed record GetGuardianStudentsQuery(Guid GuardianId) : IQuery<IReadOnlyList<GuardianStudentDto>>;
