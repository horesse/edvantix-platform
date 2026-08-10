using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.ReorderLessonMaterials;

public static class ReorderLessonMaterialsEndpoint
{
    internal static RouteHandlerBuilder MapReorderLessonMaterialsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/lessons/{lessonId:guid}/materials/reorder",
                async (Guid lessonId, [FromBody] ReorderBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReorderLessonMaterialsCommand(lessonId, body.OrderedMaterialIds), ct);
                    return Results.NoContent();
                })
            .WithName("ReorderLessonMaterials")
            .WithSummary("Set the sort order of a lesson's materials")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(CurriculumPermissions.LessonMaterials.Manage);

    public sealed record ReorderBody(IReadOnlyList<Guid> OrderedMaterialIds);
}
