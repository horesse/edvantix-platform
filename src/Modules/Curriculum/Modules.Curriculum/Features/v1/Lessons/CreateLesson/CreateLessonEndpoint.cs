using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.CreateLesson;

public static class CreateLessonEndpoint
{
    internal static RouteHandlerBuilder MapCreateLessonEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/modules/{moduleId:guid}/lessons",
                async (Guid moduleId, CreateLessonBody body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new CreateLessonCommand(moduleId, body.Title, body.Objectives, body.Content, body.DurationMinutes),
                        ct)))
            .WithName("CreateLesson")
            .WithSummary("Create a lesson in a course module")
            .RequirePermission(CurriculumPermissions.Lessons.Create)
            .WithIdempotency();

    public sealed record CreateLessonBody(string Title, string? Objectives, string? Content, int DurationMinutes);
}
