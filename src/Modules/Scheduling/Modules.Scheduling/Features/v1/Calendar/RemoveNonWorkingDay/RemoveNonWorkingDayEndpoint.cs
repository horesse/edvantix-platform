using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Calendar;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Calendar.RemoveNonWorkingDay;

public static class RemoveNonWorkingDayEndpoint
{
    internal static RouteHandlerBuilder MapRemoveNonWorkingDayEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/non-working-days/{nonWorkingDayId:guid}",
                async (Guid nonWorkingDayId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveNonWorkingDayCommand(nonWorkingDayId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveNonWorkingDay")
            .WithSummary("Remove a non-working day")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.ScheduleTemplates.Manage);
}
