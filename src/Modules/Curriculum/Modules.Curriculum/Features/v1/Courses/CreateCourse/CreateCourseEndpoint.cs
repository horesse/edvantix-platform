using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.CreateCourse;

public static class CreateCourseEndpoint
{
    internal static RouteHandlerBuilder MapCreateCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/courses",
                async (CreateCourseCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateCourse")
            .WithSummary("Create a course")
            .RequirePermission(CurriculumPermissions.Courses.Create)
            .WithIdempotency();
}
