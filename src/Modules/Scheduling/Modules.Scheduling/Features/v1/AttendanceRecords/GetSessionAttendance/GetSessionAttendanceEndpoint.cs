using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.GetSessionAttendance;

public static class GetSessionAttendanceEndpoint
{
    internal static RouteHandlerBuilder MapGetSessionAttendanceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/sessions/{sessionId:guid}/attendance",
                async (Guid sessionId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetSessionAttendanceQuery(sessionId), ct)))
            .WithName("GetSessionAttendance")
            .WithSummary("Get attendance for a session")
            .Produces<IReadOnlyList<AttendanceDto>>()
            .RequirePermission(SchedulingPermissions.Attendance.View);
}
