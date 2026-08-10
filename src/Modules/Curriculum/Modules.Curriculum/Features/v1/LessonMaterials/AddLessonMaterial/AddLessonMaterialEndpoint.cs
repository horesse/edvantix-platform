using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.LessonMaterials;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.LessonMaterials.AddLessonMaterial;

public static class AddLessonMaterialEndpoint
{
    internal static RouteHandlerBuilder MapAddLessonMaterialEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/lessons/{lessonId:guid}/materials",
                async (Guid lessonId, AddLessonMaterialBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new AddLessonMaterialCommand(
                            lessonId, body.Kind, body.Title, body.FileId, body.Url, body.VisibleToStudents),
                        ct)))
            .WithName("AddLessonMaterial")
            .WithSummary("Attach a material to a lesson")
            .RequirePermission(CurriculumPermissions.LessonMaterials.Manage)
            .WithIdempotency();

    public sealed record AddLessonMaterialBody(
        MaterialKind Kind, string Title, Guid? FileId, string? Url, bool VisibleToStudents);
}
