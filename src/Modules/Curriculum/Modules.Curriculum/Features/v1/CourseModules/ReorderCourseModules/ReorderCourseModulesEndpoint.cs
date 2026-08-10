using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.CourseModules;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.CourseModules.ReorderCourseModules;

public static class ReorderCourseModulesEndpoint
{
    internal static RouteHandlerBuilder MapReorderCourseModulesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/courses/{courseId:guid}/modules/reorder",
                async (Guid courseId, [FromBody] ReorderBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReorderCourseModulesCommand(courseId, body.OrderedModuleIds), ct);
                    return Results.NoContent();
                })
            .WithName("ReorderCourseModules")
            .WithSummary("Set the sort order of a course's modules")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(CurriculumPermissions.Courses.Update);

    public sealed record ReorderBody(IReadOnlyList<Guid> OrderedModuleIds);
}
