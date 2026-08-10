using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Contracts.v1.Courses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Curriculum.Features.v1.Courses.UpdateCourse;

public static class UpdateCourseEndpoint
{
    internal static RouteHandlerBuilder MapUpdateCourseEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPut("/courses/{courseId:guid}",
                async (Guid courseId, UpdateCourseBody body, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(
                        new UpdateCourseCommand(
                            courseId, body.SubjectId, body.Title, body.Description,
                            body.Level, body.DurationHours, body.CoverFileId),
                        ct);
                    return Results.NoContent();
                })
            .WithName("UpdateCourse")
            .WithSummary("Update a course")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(CurriculumPermissions.Courses.Update);

    public sealed record UpdateCourseBody(
        Guid SubjectId,
        string Title,
        string? Description,
        CourseLevel Level,
        int DurationHours,
        Guid? CoverFileId);
}
