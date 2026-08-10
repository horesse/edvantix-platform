using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.ActivateTeacher;

public static class ActivateTeacherEndpoint
{
    internal static RouteHandlerBuilder MapActivateTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/teachers/{teacherId:guid}/activate",
                async (Guid teacherId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ActivateTeacherCommand(teacherId), ct);
                    return Results.NoContent();
                })
            .WithName("ActivateTeacher")
            .WithSummary("Reactivate a teacher")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Teachers.Update);
    }
}
