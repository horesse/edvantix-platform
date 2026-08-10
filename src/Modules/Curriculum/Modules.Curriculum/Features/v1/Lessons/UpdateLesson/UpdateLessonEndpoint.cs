using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.UpdateLesson;

public static class UpdateLessonEndpoint
{
    internal static RouteHandlerBuilder MapUpdateLessonEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/lessons/{lessonId:guid}",
                async (Guid lessonId, UpdateLessonBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateLessonCommand(lessonId, body.Title, body.Objectives, body.Content, body.DurationMinutes),
                        ct);
                    return Results.NoContent();
                })
            .WithName("UpdateLesson")
            .WithSummary("Update a lesson")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Lessons.Update);

    public sealed record UpdateLessonBody(string Title, string? Objectives, string? Content, int DurationMinutes);
}
