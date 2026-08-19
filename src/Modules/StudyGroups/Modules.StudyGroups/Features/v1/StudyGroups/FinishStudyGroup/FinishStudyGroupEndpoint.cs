using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.FinishStudyGroup;

public static class FinishStudyGroupEndpoint
{
    internal static RouteHandlerBuilder MapFinishStudyGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/study-groups/{studyGroupId:guid}/finish",
                async (Guid studyGroupId, FinishStudyGroupCommand? body, IMediator mediator, CancellationToken ct) =>
                {
                    var command = (body ?? new FinishStudyGroupCommand(studyGroupId)) with { StudyGroupId = studyGroupId };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("FinishStudyGroup")
            .WithSummary("Finish an active study group")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Archive);
    }
}
