using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers.RemoveGroupTeacher;

public static class RemoveGroupTeacherEndpoint
{
    internal static RouteHandlerBuilder MapRemoveGroupTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/study-groups/{studyGroupId:guid}/teachers/{teacherId:guid}",
                async (Guid studyGroupId, Guid teacherId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveGroupTeacherCommand(studyGroupId, teacherId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveGroupTeacher")
            .WithSummary("Remove a teacher from a study group's roster")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Update);
    }
}
