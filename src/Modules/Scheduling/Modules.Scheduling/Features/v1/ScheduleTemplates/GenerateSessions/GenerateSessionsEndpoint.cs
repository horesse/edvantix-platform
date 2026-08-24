using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.ScheduleTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.ScheduleTemplates.GenerateSessions;

public static class GenerateSessionsEndpoint
{
    internal static RouteHandlerBuilder MapGenerateSessionsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/schedule-templates/{scheduleTemplateId:guid}/generate",
                async (Guid scheduleTemplateId, int? horizonWeeks, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GenerateSessionsCommand(scheduleTemplateId, horizonWeeks), ct)))
            .WithName("GenerateSessions")
            .WithSummary("Generate sessions from a schedule template")
            .Produces<GenerationResultDto>()
            // Separate from Sessions.Create on purpose — touches hundreds of rows in one call, see
            // docs/02 Модули/Scheduling.md → "Права".
            .RequirePermission(SchedulingPermissions.Sessions.Generate)
            .WithIdempotency();
}
