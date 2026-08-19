using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.GetGroupAttendanceReport;

public static class GetGroupAttendanceReportEndpoint
{
    internal static RouteHandlerBuilder MapGetGroupAttendanceReportEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/study-groups/{studyGroupId:guid}/attendance-report",
                async (Guid studyGroupId, DateOnly from, DateOnly to, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetGroupAttendanceReportQuery(studyGroupId, from, to), ct)))
            .WithName("GetGroupAttendanceReport")
            .WithSummary("Get an attendance summary for a study group over a period")
            .Produces<AttendanceReportDto>()
            .RequirePermission(SchedulingPermissions.Attendance.View);
}
