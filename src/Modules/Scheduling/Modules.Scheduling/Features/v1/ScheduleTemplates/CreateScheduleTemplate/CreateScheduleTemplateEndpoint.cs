using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.CreateScheduleTemplate;

public static class CreateScheduleTemplateEndpoint
{
    internal static RouteHandlerBuilder MapCreateScheduleTemplateEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/study-groups/{studyGroupId:guid}/schedule-templates",
                async (Guid studyGroupId, CreateScheduleTemplateBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new CreateScheduleTemplateCommand(
                            studyGroupId,
                            body.DayOfWeek,
                            body.StartTime,
                            body.DurationMinutes,
                            body.RoomId,
                            body.TeacherId,
                            body.ValidFrom,
                            body.ValidTo),
                        ct)))
            .WithName("CreateScheduleTemplate")
            .WithSummary("Create a schedule template for a study group")
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.Manage)
            .WithIdempotency();

    public sealed record CreateScheduleTemplateBody(
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        int DurationMinutes,
        Guid? RoomId,
        Guid? TeacherId,
        DateOnly ValidFrom,
        DateOnly? ValidTo);
}
