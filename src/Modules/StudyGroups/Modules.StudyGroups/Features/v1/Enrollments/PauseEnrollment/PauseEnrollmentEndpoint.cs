using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Enrollments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Enrollments.PauseEnrollment;

public static class PauseEnrollmentEndpoint
{
    internal static RouteHandlerBuilder MapPauseEnrollmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/enrollments/{enrollmentId:guid}/pause",
                async (Guid enrollmentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new PauseEnrollmentCommand(enrollmentId), ct);
                    return Results.NoContent();
                })
            .WithName("PauseEnrollment")
            .WithSummary("Pause an active enrollment")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            // Pause/Resume are roster edits, not new enrollments or removals — the permission
            // table (docs/01 Архитектура/Модель прав доступа.md) has no separate "Enrollments.Update",
            // so this reuses Create the same way Curriculum's CourseModule CRUD reuses Courses.Update.
            .RequirePermission(StudyGroupsPermissions.Enrollments.Create);
    }
}
