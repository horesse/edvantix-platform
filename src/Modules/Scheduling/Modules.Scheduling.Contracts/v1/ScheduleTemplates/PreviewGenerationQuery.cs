using FSH.Modules.Scheduling.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;

public sealed record PreviewGenerationQuery(Guid ScheduleTemplateId, int? HorizonWeeks) : IQuery<GenerationPreviewDto>;
