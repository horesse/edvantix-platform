using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.DeleteStudent;

public static class DeleteStudentEndpoint
{
    internal static RouteHandlerBuilder MapDeleteStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/students/{studentId:guid}",
                async (Guid studentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteStudentCommand(studentId), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteStudent")
            .WithSummary("Delete a student")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequirePermission(PeoplePermissions.Students.Delete);
    }
}
