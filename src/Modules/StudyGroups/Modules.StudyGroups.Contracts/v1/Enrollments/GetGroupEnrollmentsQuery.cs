using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.Enrollments;

public sealed record GetGroupEnrollmentsQuery(Guid StudyGroupId) : IQuery<IReadOnlyList<GroupEnrollmentDto>>;
