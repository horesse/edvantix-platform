using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.v1.Students;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.People.Features.v1.Students.RestoreStudent;

public static class RestoreStudentEndpoint
{
    internal static RouteHandlerBuilder MapRestoreStudentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/students/{studentId:guid}/restore",
                async (Guid studentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RestoreStudentCommand(studentId), ct);
                    return Results.NoContent();
                })
            .WithName("RestoreStudent")
            .WithSummary("Restore an archived student back to Active")
            .Produces(StatusCodes.Status204NoContent)
            .RequirePermission(PeoplePermissions.Students.Update);
    }
}
