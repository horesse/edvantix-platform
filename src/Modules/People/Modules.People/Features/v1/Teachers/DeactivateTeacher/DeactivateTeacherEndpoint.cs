using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.DeactivateTeacher;

public static class DeactivateTeacherEndpoint
{
    internal static RouteHandlerBuilder MapDeactivateTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/teachers/{teacherId:guid}/deactivate",
                async (Guid teacherId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeactivateTeacherCommand(teacherId), ct);
                    return Results.NoContent();
                })
            .WithName("DeactivateTeacher")
            .WithSummary("Deactivate a teacher")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Teachers.Update);
    }
}
