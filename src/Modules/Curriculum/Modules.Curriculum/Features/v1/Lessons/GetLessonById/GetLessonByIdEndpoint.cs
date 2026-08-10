using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.v1.Lessons;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Lessons.GetLessonById;

public static class GetLessonByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetLessonByIdEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/lessons/{lessonId:guid}",
                async (Guid lessonId, IMediator mediator, CancellationToken ct) =>
                    await mediator.Send(new GetLessonByIdQuery(lessonId), ct))
            .WithName("GetLessonById")
            .WithSummary("Get a lesson")
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Lessons.View);
}
