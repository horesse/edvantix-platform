using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.GetStudentEnrollments;

public static class GetStudentEnrollmentsEndpoint
{
    internal static RouteHandlerBuilder MapGetStudentEnrollmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/students/{studentId:guid}/enrollments",
                (Guid studentId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetStudentEnrollmentsQuery(studentId), ct))
            .WithName("GetStudentEnrollments")
            .WithSummary("List a student's group enrollment history, including finished/left groups")
            .RequirePermission(StudyGroupsPermissions.Enrollments.View);
    }
}
