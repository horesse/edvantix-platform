using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.DeleteCourse;

public static class DeleteCourseEndpoint
{
    internal static RouteHandlerBuilder MapDeleteCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/courses/{courseId:guid}",
                async (Guid courseId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteCourseCommand(courseId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteCourse")
            .WithSummary("Move a course to trash")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Delete);
}
