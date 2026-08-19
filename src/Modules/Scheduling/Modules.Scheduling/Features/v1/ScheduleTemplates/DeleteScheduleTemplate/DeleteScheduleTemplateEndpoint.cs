using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.DeleteScheduleTemplate;

public static class DeleteScheduleTemplateEndpoint
{
    internal static RouteHandlerBuilder MapDeleteScheduleTemplateEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/schedule-templates/{scheduleTemplateId:guid}",
                async (Guid scheduleTemplateId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteScheduleTemplateCommand(scheduleTemplateId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteScheduleTemplate")
            .WithSummary("Delete a schedule template")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.Manage);
}
