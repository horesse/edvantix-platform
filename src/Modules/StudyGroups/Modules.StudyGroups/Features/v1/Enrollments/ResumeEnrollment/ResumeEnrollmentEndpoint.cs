using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.ResumeEnrollment;

public static class ResumeEnrollmentEndpoint
{
    internal static RouteHandlerBuilder MapResumeEnrollmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/enrollments/{enrollmentId:guid}/resume",
                async (Guid enrollmentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ResumeEnrollmentCommand(enrollmentId), ct);
                    return Results.NoContent();
                })
            .WithName("ResumeEnrollment")
            .WithSummary("Resume a paused enrollment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(StudyGroupsPermissions.Enrollments.Create);
    }
}
