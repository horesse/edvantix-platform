using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.ReorderLessons;

public static class ReorderLessonsEndpoint
{
    internal static RouteHandlerBuilder MapReorderLessonsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/modules/{moduleId:guid}/lessons/reorder",
                async (Guid moduleId, [FromBody] ReorderBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReorderLessonsCommand(moduleId, body.OrderedLessonIds), ct);
                    return Results.NoContent();
                })
            .WithName("ReorderLessons")
            .WithSummary("Set the sort order of a module's lessons")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(CurriculumPermissions.Lessons.Update);

    public sealed record ReorderBody(IReadOnlyList<Guid> OrderedLessonIds);
}
