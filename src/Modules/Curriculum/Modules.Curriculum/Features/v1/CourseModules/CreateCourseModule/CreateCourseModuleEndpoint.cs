using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.CreateCourseModule;

public static class CreateCourseModuleEndpoint
{
    internal static RouteHandlerBuilder MapCreateCourseModuleEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/courses/{courseId:guid}/modules",
                async (Guid courseId, CreateCourseModuleBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new CreateCourseModuleCommand(courseId, body.Title, body.Description), ct)))
            .WithName("CreateCourseModule")
            .WithSummary("Create a module (section) in a course")
            .RequirePermission(CurriculumPermissions.Courses.Update)
            .WithIdempotency();

    public sealed record CreateCourseModuleBody(string Title, string? Description);
}
