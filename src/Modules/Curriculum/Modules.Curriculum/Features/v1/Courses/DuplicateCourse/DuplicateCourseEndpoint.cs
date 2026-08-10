using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.DuplicateCourse;

public static class DuplicateCourseEndpoint
{
    internal static RouteHandlerBuilder MapDuplicateCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/courses/{courseId:guid}/duplicate",
                async (Guid courseId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new DuplicateCourseCommand(courseId), ct)))
            .WithName("DuplicateCourse")
            .WithSummary("Deep-clone a course (modules, lessons, materials) as a new draft")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Create)
            .WithIdempotency();
}
