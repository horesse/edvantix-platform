using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.GetCourseById;

public static class GetCourseByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetCourseByIdEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/courses/{courseId:guid}",
                async (Guid courseId, IMediator mediator, CancellationToken ct) =>
                    await mediator.Send(new GetCourseByIdQuery(courseId), ct))
            .WithName("GetCourseById")
            .WithSummary("Get a course with its modules and lessons")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.View);
}
