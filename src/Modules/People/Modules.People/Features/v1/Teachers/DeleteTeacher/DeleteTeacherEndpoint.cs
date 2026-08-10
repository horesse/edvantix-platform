using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Teachers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Teachers.DeleteTeacher;

public static class DeleteTeacherEndpoint
{
    internal static RouteHandlerBuilder MapDeleteTeacherEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/teachers/{teacherId:guid}",
                async (Guid teacherId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteTeacherCommand(teacherId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteTeacher")
            .WithSummary("Delete a teacher")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PeoplePermissions.Teachers.Delete);
    }
}
