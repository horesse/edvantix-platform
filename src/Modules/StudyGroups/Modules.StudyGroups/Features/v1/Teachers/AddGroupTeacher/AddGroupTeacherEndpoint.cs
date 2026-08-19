using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.StudyGroups.Features.v1.Teachers.AddGroupTeacher;

public static class AddGroupTeacherEndpoint
{
    internal static RouteHandlerBuilder MapAddGroupTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/study-groups/{studyGroupId:guid}/teachers",
                async (Guid studyGroupId, AddGroupTeacherCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    var command = body with { StudyGroupId = studyGroupId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("AddGroupTeacher")
            .WithSummary("Add a teacher (assistant/substitute/co-primary) to a study group's roster")
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            // Gated by StudyGroups.Update, not a dedicated resource — staffing is part of editing
            // the group, same reasoning as Curriculum's CourseModule CRUD under Courses.Update.
            .RequirePermission(StudyGroupsPermissions.StudyGroups.Update);
    }
}
