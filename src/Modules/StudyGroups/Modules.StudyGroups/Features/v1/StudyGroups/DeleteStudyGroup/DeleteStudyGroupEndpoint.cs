using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.DeleteStudyGroup;

public static class DeleteStudyGroupEndpoint
{
    internal static RouteHandlerBuilder MapDeleteStudyGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/study-groups/{studyGroupId:guid}",
                async (Guid studyGroupId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteStudyGroupCommand(studyGroupId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteStudyGroup")
            .WithSummary("Delete a study group")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Delete);
    }
}
