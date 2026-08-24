using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.UpdateScheduleTemplate;

public static class UpdateScheduleTemplateEndpoint
{
    internal static RouteHandlerBuilder MapUpdateScheduleTemplateEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/schedule-templates/{scheduleTemplateId:guid}",
                async (Guid scheduleTemplateId, UpdateScheduleTemplateBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateScheduleTemplateCommand(
                            scheduleTemplateId,
                            body.DayOfWeek,
                            body.StartTime,
                            body.DurationMinutes,
                            body.RoomId,
                            body.TeacherId,
                            body.ValidFrom,
                            body.ValidTo,
                            body.IsActive),
                        ct);
                    return Results.NoContent();
                })
            .WithName("UpdateScheduleTemplate")
            .WithSummary("Update a schedule template")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.Manage);

    public sealed record UpdateScheduleTemplateBody(
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        int DurationMinutes,
        Guid? RoomId,
        Guid? TeacherId,
        DateOnly ValidFrom,
        DateOnly? ValidTo,
        bool IsActive);
}
