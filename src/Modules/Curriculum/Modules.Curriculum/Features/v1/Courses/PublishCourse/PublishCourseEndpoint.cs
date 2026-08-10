using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.PublishCourse;

public static class PublishCourseEndpoint
{
    internal static RouteHandlerBuilder MapPublishCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/courses/{courseId:guid}/publish",
                async (Guid courseId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new PublishCourseCommand(courseId), ct);
                    return Results.NoContent();
                })
            .WithName("PublishCourse")
            .WithSummary("Publish a course — required before a study group can be created against it")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .RequirePermission(CurriculumPermissions.Courses.Publish);
}
