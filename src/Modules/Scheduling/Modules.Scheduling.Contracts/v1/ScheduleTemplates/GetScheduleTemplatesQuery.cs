using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

public sealed record GetScheduleTemplatesQuery(Guid StudyGroupId) : IQuery<IReadOnlyList<ScheduleTemplateDto>>;
