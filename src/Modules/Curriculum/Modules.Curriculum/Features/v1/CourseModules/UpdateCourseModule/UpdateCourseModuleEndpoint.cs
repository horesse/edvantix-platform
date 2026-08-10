using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.UpdateCourseModule;

public static class UpdateCourseModuleEndpoint
{
    internal static RouteHandlerBuilder MapUpdateCourseModuleEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/modules/{moduleId:guid}",
                async (Guid moduleId, UpdateCourseModuleBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new UpdateCourseModuleCommand(moduleId, body.Title, body.Description), ct);
                    return Results.NoContent();
                })
            .WithName("UpdateCourseModule")
            .WithSummary("Update a course module")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Update);

    public sealed record UpdateCourseModuleBody(string Title, string? Description);
}
