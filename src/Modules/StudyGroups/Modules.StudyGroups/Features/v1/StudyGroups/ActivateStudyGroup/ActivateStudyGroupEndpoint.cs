using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.StudyGroups;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.StudyGroups.ActivateStudyGroup;

public static class ActivateStudyGroupEndpoint
{
    internal static RouteHandlerBuilder MapActivateStudyGroupEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/study-groups/{studyGroupId:guid}/activate",
                async (Guid studyGroupId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ActivateStudyGroupCommand(studyGroupId), ct);
                    return Results.NoContent();
                })
            .WithName("ActivateStudyGroup")
            .WithSummary("Activate a forming study group")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            // "Archive" gates all lifecycle transitions on StudyGroups (activate/finish/cancel) —
            // matches the permission table in docs/02 Модули/StudyGroups.md, same shape as
            // Curriculum's Courses.Publish gating both Publish and Archive.
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Archive);
    }
}
