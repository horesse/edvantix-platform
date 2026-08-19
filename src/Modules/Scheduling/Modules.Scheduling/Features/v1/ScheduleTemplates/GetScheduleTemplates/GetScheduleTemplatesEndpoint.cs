using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.GetScheduleTemplates;

public static class GetScheduleTemplatesEndpoint
{
    internal static RouteHandlerBuilder MapGetScheduleTemplatesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/study-groups/{studyGroupId:guid}/schedule-templates",
                async (Guid studyGroupId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetScheduleTemplatesQuery(studyGroupId), ct)))
            .WithName("GetScheduleTemplates")
            .WithSummary("List schedule templates for a study group")
            .Produces<IReadOnlyList<ScheduleTemplateDto>>()
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.View);
}
