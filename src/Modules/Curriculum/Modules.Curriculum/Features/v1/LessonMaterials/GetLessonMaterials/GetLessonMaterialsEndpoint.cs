using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.GetLessonMaterials;

public static class GetLessonMaterialsEndpoint
{
    internal static RouteHandlerBuilder MapGetLessonMaterialsEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/lessons/{lessonId:guid}/materials",
                async (Guid lessonId, IMediator mediator, CancellationToken ct) =>
                    await mediator.Send(new GetLessonMaterialsQuery(lessonId), ct))
            .WithName("GetLessonMaterials")
            .WithSummary("List a lesson's materials")
            .RequirePermission(CurriculumPermissions.LessonMaterials.View);
}
