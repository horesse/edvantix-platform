using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.GetStudyGroupById;

public static class GetStudyGroupByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetStudyGroupByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/study-groups/{studyGroupId:guid}",
                (Guid studyGroupId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetStudyGroupByIdQuery(studyGroupId), ct))
            .WithName("GetStudyGroupById")
            .WithSummary("Get a study group by id, with its roster")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(StudyGroupsPermissions.StudyGroups.View);
    }
}
