using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.StudyGroups.GetGroupCourseProgress;

/// <summary>Mapped under "/study-groups" — StudyGroups' resource name — the same cross-module route
/// ownership already used by <c>GetGroupAttendanceReportEndpoint</c>
/// ("/study-groups/{id}/attendance-report"). Gated by Scheduling's own <c>Sessions.View</c>.</summary>
public static class GetGroupCourseProgressEndpoint
{
    internal static RouteHandlerBuilder MapGetGroupCourseProgressEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/study-groups/{studyGroupId:guid}/course-progress",
                async (Guid studyGroupId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetGroupCourseProgressQuery(studyGroupId), ct)))
            .WithName("GetGroupCourseProgress")
            .WithSummary("Get a study group's progress through its course program")
            .Produces<CourseProgressDto>()
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(SchedulingPermissions.Sessions.View);
}
