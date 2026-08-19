using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.GetGroupEnrollments;

public static class GetGroupEnrollmentsEndpoint
{
    internal static RouteHandlerBuilder MapGetGroupEnrollmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/study-groups/{studyGroupId:guid}/enrollments",
                (Guid studyGroupId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetGroupEnrollmentsQuery(studyGroupId), ct))
            .WithName("GetGroupEnrollments")
            .WithSummary("List a study group's enrollments (all statuses)")
            .RequirePermission(StudyGroupsPermissions.Enrollments.View);
    }
}
