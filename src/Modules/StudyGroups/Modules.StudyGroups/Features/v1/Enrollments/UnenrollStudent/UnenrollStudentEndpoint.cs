using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.UnenrollStudent;

public static class UnenrollStudentEndpoint
{
    internal static RouteHandlerBuilder MapUnenrollStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/study-groups/{studyGroupId:guid}/enrollments/{enrollmentId:guid}",
                async (Guid studyGroupId, Guid enrollmentId, string? reason, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UnenrollStudentCommand(studyGroupId, enrollmentId, Reason: reason), ct);
                    return Results.NoContent();
                })
            .WithName("UnenrollStudent")
            .WithSummary("Unenroll a student from a study group (marks the enrollment Left)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(StudyGroupsPermissions.Enrollments.Delete);
    }
}
