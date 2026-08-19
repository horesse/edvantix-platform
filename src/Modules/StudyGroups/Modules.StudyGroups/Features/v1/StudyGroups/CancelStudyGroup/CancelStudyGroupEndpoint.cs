using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.CancelStudyGroup;

public static class CancelStudyGroupEndpoint
{
    internal static RouteHandlerBuilder MapCancelStudyGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/study-groups/{studyGroupId:guid}/cancel",
                async (Guid studyGroupId, CancelStudyGroupCommand? body, IMediator mediator, CancellationToken ct) =>
                {
                    var command = (body ?? new CancelStudyGroupCommand(studyGroupId)) with { StudyGroupId = studyGroupId };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("CancelStudyGroup")
            .WithSummary("Cancel a forming or active study group")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Archive);
    }
}
