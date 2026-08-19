using FSH.Modules.StudyGroups.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;

public sealed record GetStudyGroupByIdQuery(Guid StudyGroupId) : IQuery<StudyGroupDetailDto>;
