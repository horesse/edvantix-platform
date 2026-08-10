using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.DeleteCourseModule;

public static class DeleteCourseModuleEndpoint
{
    internal static RouteHandlerBuilder MapDeleteCourseModuleEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/modules/{moduleId:guid}",
                async (Guid moduleId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteCourseModuleCommand(moduleId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteCourseModule")
            .WithSummary("Delete a course module (cascades its lessons and their materials)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Update);
}
