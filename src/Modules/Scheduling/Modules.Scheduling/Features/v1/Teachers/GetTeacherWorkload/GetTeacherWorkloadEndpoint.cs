using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Teachers.GetTeacherWorkload;

/// <summary>Mapped under "/teachers" — People's resource name — the same cross-module route
/// ownership already used by <c>GetStudentAttendanceEndpoint</c> ("/students/{id}/attendance").
/// Gated by Scheduling's own <c>Sessions.View</c>, not a People permission, for the same reason.</summary>
public static class GetTeacherWorkloadEndpoint
{
    internal static RouteHandlerBuilder MapGetTeacherWorkloadEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/teachers/{teacherId:guid}/workload",
                async (Guid teacherId, DateOnly? from, DateOnly? to, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetTeacherWorkloadQuery(teacherId, from, to), ct)))
            .WithName("GetTeacherWorkload")
            .WithSummary("Get a teacher's group/session workload for a period")
            .Produces<TeacherWorkloadDto>()
            .RequirePermission(SchedulingPermissions.Sessions.View);
}
