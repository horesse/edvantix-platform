using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.GetMyStudyGroups;

public static class GetMyStudyGroupsEndpoint
{
    internal static RouteHandlerBuilder MapGetMyStudyGroupsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/study-groups/my",
                (IMediator mediator, CancellationToken ct) => mediator.Send(new GetMyStudyGroupsQuery(), ct))
            .WithName("GetMyStudyGroups")
            .WithSummary("List the caller's own study groups (as teacher or student)")
            .RequirePermission(StudyGroupsPermissions.StudyGroups.ViewOwn);
    }
}
