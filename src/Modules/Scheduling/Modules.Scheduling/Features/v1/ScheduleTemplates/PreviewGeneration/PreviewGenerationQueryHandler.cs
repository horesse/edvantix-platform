using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.PreviewGeneration;

public sealed class PreviewGenerationQueryHandler(
    SchedulingDbContext dbContext,
    IScheduleGeneratorService generatorService)
    : IQueryHandler<PreviewGenerationQuery, GenerationPreviewDto>
{
    public async ValueTask<GenerationPreviewDto> Handle(PreviewGenerationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var template = await dbContext.ScheduleTemplates
            .FirstOrDefaultAsync(t => t.Id == query.ScheduleTemplateId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ScheduleTemplate {query.ScheduleTemplateId} not found.");

        var plan = await generatorService.PlanAsync(
                template, query.HorizonWeeks ?? SchedulingDefaults.DefaultHorizonWeeks, cancellationToken)
            .ConfigureAwait(false);

        return new GenerationPreviewDto(
            template.Id,
            plan.ToCreate.Select(o => new GeneratedSessionPreviewDto(o.LocalDate, o.StartUtc, o.EndUtc)).ToList(),
            plan.Skipped.Select(s => new GenerationSkipDto(s.LocalDate, s.Reason, s.Conflicts)).ToList());
    }
}
