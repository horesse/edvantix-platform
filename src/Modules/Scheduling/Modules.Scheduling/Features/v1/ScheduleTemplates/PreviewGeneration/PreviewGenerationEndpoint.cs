using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.PreviewGeneration;

public static class PreviewGenerationEndpoint
{
    internal static RouteHandlerBuilder MapPreviewGenerationEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/schedule-templates/{scheduleTemplateId:guid}/preview",
                async (Guid scheduleTemplateId, int? horizonWeeks, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new PreviewGenerationQuery(scheduleTemplateId, horizonWeeks), ct)))
            .WithName("PreviewGeneration")
            .WithSummary("Preview what generating sessions from a template would create")
            .Produces<GenerationPreviewDto>()
            .RequirePermission(SchedulingPermissions.Sessions.Generate);
}
