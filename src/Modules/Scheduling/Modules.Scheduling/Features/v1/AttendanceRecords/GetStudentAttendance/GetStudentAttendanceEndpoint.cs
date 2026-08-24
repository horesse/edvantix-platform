using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Attendance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.AttendanceRecords.GetStudentAttendance;

public static class GetStudentAttendanceEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentAttendanceEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/students/{studentId:guid}/attendance",
                async (Guid studentId, DateOnly? from, DateOnly? to, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetStudentAttendanceQuery(studentId, from, to), ct)))
            .WithName("GetStudentAttendance")
            .WithSummary("Get a student's attendance history")
            .Produces<IReadOnlyList<AttendanceDto>>()
            .RequirePermission(SchedulingPermissions.Attendance.View);
}
