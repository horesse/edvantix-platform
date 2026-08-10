using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.DeleteLesson;

public static class DeleteLessonEndpoint
{
    internal static RouteHandlerBuilder MapDeleteLessonEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapDelete("/lessons/{lessonId:guid}",
                async (Guid lessonId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteLessonCommand(lessonId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteLesson")
            .WithSummary("Delete a lesson (cascades its materials)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Lessons.Delete);
}
