using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.UpdateStudyGroup;

public static class UpdateStudyGroupEndpoint
{
    internal static RouteHandlerBuilder MapUpdateStudyGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/study-groups/{studyGroupId:guid}",
                async (Guid studyGroupId, UpdateStudyGroupCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { StudyGroupId = studyGroupId };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateStudyGroup")
            .WithSummary("Update a study group")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Update);
    }
}
