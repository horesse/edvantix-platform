using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.GetNonWorkingDays;

public static class GetNonWorkingDaysEndpoint
{
    internal static RouteHandlerBuilder MapGetNonWorkingDaysEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/non-working-days",
                async (DateOnly? from, DateOnly? to, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetNonWorkingDaysQuery(from, to), ct)))
            .WithName("GetNonWorkingDays")
            .WithSummary("List non-working days")
            .Produces<IReadOnlyList<NonWorkingDayDto>>()
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.View);
}
