using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.RestoreCourse;

public static class RestoreCourseEndpoint
{
    internal static RouteHandlerBuilder MapRestoreCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/courses/{courseId:guid}/restore",
                async (Guid courseId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new RestoreCourseCommand(courseId), ct)))
            .WithName("RestoreCourse")
            .WithSummary("Restore a course from trash")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Restore);
}
