using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.ArchiveCourse;

public static class ArchiveCourseEndpoint
{
    internal static RouteHandlerBuilder MapArchiveCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/courses/{courseId:guid}/archive",
                async (Guid courseId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ArchiveCourseCommand(courseId), ct);
                    return Results.NoContent();
                })
            .WithName("ArchiveCourse")
            .WithSummary("Archive a course — blocks new study groups, existing ones keep running")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Publish);
}
