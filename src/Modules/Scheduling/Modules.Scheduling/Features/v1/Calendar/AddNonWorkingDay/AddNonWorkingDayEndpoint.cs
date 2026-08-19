using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.AddNonWorkingDay;

public static class AddNonWorkingDayEndpoint
{
    internal static RouteHandlerBuilder MapAddNonWorkingDayEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/non-working-days",
                async (AddNonWorkingDayCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("AddNonWorkingDay")
            .WithSummary("Add a non-working day")
            // Docs → "Права" lists 4 resources (Sessions/Attendance/Rooms/ScheduleTemplates), no
            // dedicated one for the school calendar — non-working days are schedule-generation
            // configuration, same category as templates, so they're gated behind the same right.
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.Manage)
            .WithIdempotency();
}
