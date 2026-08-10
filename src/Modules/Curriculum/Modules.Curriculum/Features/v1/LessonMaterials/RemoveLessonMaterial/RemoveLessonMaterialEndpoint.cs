using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.RemoveLessonMaterial;

public static class RemoveLessonMaterialEndpoint
{
    internal static RouteHandlerBuilder MapRemoveLessonMaterialEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/materials/{materialId:guid}",
                async (Guid materialId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveLessonMaterialCommand(materialId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveLessonMaterial")
            .WithSummary("Remove a material from a lesson")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.LessonMaterials.Manage);
}
